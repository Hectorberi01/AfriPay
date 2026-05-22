namespace AfriPay.Domain.Disputes;

public enum DisputeStatus
{
    Open, 
    EvidenceSubmitted, 
    UnderReview, 
    Won,
    Lost, 
    Cancelled, 
    Expired
}