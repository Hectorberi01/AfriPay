# AfriPay — Déploiement Ansible

Infrastructure as Code pour le déploiement automatisé d'AfriPay en production.

## Prérequis

```bash
pip install ansible ansible-lint
ansible-galaxy collection install community.docker community.general ansible.posix
```

## Structure

```
ansible/
├── ansible.cfg                        # Configuration Ansible
├── inventory/
│   ├── production/
│   │   ├── hosts.yml                  # Serveurs de production
│   │   └── group_vars/
│   │       ├── all.yml                # Variables globales
│   │       └── vault.yml              # Secrets (chiffrer avec Vault)
│   └── staging/
│       └── hosts.yml
├── playbooks/
│   ├── provision.yml                  # Provisionning initial (1 seule fois)
│   ├── deploy.yml                     # Déploiement applicatif
│   ├── rollback.yml                   # Retour en arrière
│   └── maintenance.yml                # Mode maintenance
└── roles/
    ├── common/                        # Durcissement OS, swap, UFW, fail2ban
    ├── docker/                        # Installation Docker CE + Compose V2
    ├── afripay/                       # Stack applicative (API + BDD + Redis)
    ├── nginx/                         # Reverse proxy + TLS
    ├── certbot/                       # Certificats Let's Encrypt
    └── monitoring/                    # Prometheus + Grafana + Alertmanager
```

## 1. Configuration initiale

### Chiffrer les secrets

```bash
# Créer le fichier de secrets
cp inventory/production/group_vars/vault.yml.example \
   inventory/production/group_vars/vault.yml

# Éditer les valeurs (voir vault.yml)
nano inventory/production/group_vars/vault.yml

# Chiffrer avec Ansible Vault
ansible-vault encrypt inventory/production/group_vars/vault.yml

# Stocker le mot de passe vault
echo "MON_MOT_DE_PASSE_VAULT" > ~/.ansible/vault_pass
chmod 600 ~/.ansible/vault_pass
```

### Configurer la clé SSH

```bash
# Générer une clé dédiée
ssh-keygen -t ed25519 -f ~/.ssh/afripay_prod -C "afripay-ansible"

# Copier sur le serveur
ssh-copy-id -i ~/.ssh/afripay_prod.pub afripay@IP_DU_SERVEUR
```

## 2. Provisionner un nouveau serveur

```bash
# Provisionning complet (première installation)
ansible-playbook playbooks/provision.yml

# Provisionner seulement Docker
ansible-playbook playbooks/provision.yml --tags docker

# Vérifier sans exécuter (dry-run)
ansible-playbook playbooks/provision.yml --check --diff
```

## 3. Déployer une mise à jour

```bash
# Déployer la version latest (main)
ansible-playbook playbooks/deploy.yml

# Déployer un tag spécifique
ansible-playbook playbooks/deploy.yml -e "image_tag=sha-abc1234"

# Déployer sans migrations
ansible-playbook playbooks/deploy.yml --skip-tags migrate

# Déployer sur staging
ansible-playbook playbooks/deploy.yml -i inventory/staging
```

## 4. Rollback

```bash
# Revenir à une version précédente
ansible-playbook playbooks/rollback.yml -e "rollback_tag=sha-abc1234"
```

## 5. Mode maintenance

```bash
# Activer le mode maintenance
ansible-playbook playbooks/maintenance.yml -e "maintenance=on"

# Désactiver le mode maintenance
ansible-playbook playbooks/maintenance.yml -e "maintenance=off"
```

## 6. Commandes utiles

```bash
# Tester la connectivité
ansible all -m ping

# Vérifier les faits du serveur
ansible api -m setup | grep ansible_distribution

# Exécuter une commande ad-hoc
ansible api -m shell -a "docker ps"

# Voir les logs de l'API
ansible api -m shell -a "docker compose -f /opt/afripay/docker-compose.yml logs --tail=100 afripay-api"

# Vérifier la santé
ansible api -m uri -a "url=https://api.afripay.io/health"
```

## Tags disponibles

| Tag | Description |
|---|---|
| `common` | Configuration de base du serveur |
| `docker` | Installation Docker |
| `nginx` | Configuration Nginx |
| `certbot` | Certificats SSL |
| `afripay` | Déploiement de l'application |
| `monitoring` | Stack Prometheus/Grafana |
| `pull` | Téléchargement des images |
| `migrate` | Migrations base de données |
| `restart` | Redémarrage des services |
| `healthcheck` | Vérification de santé |
| `config` | Mise à jour des fichiers de config |

## Variables d'environnement CI/CD

### Secrets communs (Settings → Secrets → Actions)

| Secret | Description |
|---|---|
| `ANSIBLE_VAULT_PASS` | Mot de passe Ansible Vault (commun aux deux envs) |

### Secrets environnement `staging` (Settings → Environments → staging)

| Secret | Description |
|---|---|
| `STAGING_HOST` | IP ou hostname du serveur staging |
| `STAGING_SSH_KEY` | Clé privée SSH (`~/.ssh/afripay_staging`) |

### Secrets environnement `production` (Settings → Environments → production)

| Secret | Description |
|---|---|
| `PROD_HOST` | IP ou hostname du serveur de production |
| `PROD_SSH_KEY` | Clé privée SSH (`~/.ssh/afripay_prod`) |

### Configurer les vault.yml

```bash
# Staging : renseigner les secrets puis chiffrer
nano inventory/staging/group_vars/vault.yml
ansible-vault encrypt inventory/staging/group_vars/vault.yml

# Production : même procédure
ansible-vault encrypt inventory/production/group_vars/vault.yml
```