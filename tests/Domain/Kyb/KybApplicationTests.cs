using AfriPay.Domain.Kyb;
using FluentAssertions;

namespace AfriPay.Tests.Domain.Kyb;

public sealed class KybApplicationTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();

    private static KybApplication CreateFresh() =>
        KybApplication.Create(MerchantId, "sn");

    private static KybApplication CreateDraft()
    {
        var kyb = CreateFresh();
        kyb.SetBusinessInfo("AfriShop SARL", "SN-REG-001", "SN-TAX-001", "sarl", "https://afrishop.sn");
        kyb.SetLegalRepresentative("Mamadou Diallo", "mamadou@afrishop.sn");
        return kyb;
    }

    private static KybApplication CreateReadyToSubmit()
    {
        var kyb = CreateDraft();
        kyb.AddDocument(DocumentType.BusinessRegistration, "reg.pdf", "storage/reg.pdf", "application/pdf", 102_400);
        kyb.AddDocument(DocumentType.OwnerIdentity, "id.jpg", "storage/id.jpg", "image/jpeg", 204_800);
        return kyb;
    }

    //Create

    [Fact]
    public void Create_InitializesWithNotStartedStatus()
    {
        var kyb = CreateFresh();

        kyb.Status.Should().Be(KybStatus.NotStarted);
        kyb.MerchantId.Should().Be(MerchantId);
        kyb.Country.Should().Be("SN");
        kyb.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_NormalizeCountryToUpperCase()
    {
        var kyb = KybApplication.Create(MerchantId, "ci");

        kyb.Country.Should().Be("CI");
    }

    // SetBusinessInfo

    [Fact]
    public void SetBusinessInfo_NotStartedKyb_SetsFieldsAndDraftStatus()
    {
        var kyb = CreateFresh();

        kyb.SetBusinessInfo("AfriShop SARL", "SN-REG-001", "SN-TAX-001", "sarl", "https://afrishop.sn");

        kyb.Status.Should().Be(KybStatus.Draft);
        kyb.LegalName.Should().Be("AfriShop SARL");
        kyb.RegistrationNumber.Should().Be("SN-REG-001");
        kyb.TaxId.Should().Be("SN-TAX-001");
        kyb.BusinessType.Should().Be("sarl");
        kyb.Website.Should().Be("https://afrishop.sn");
    }

    [Fact]
    public void SetBusinessInfo_SubmittedKyb_ThrowsKybDomainException()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();

        var act = () => kyb.SetBusinessInfo("New Name", null, null, null, null);

        act.Should().Throw<KybDomainException>()
            .WithMessage("*cannot be modified*");
    }

    [Fact]
    public void SetBusinessInfo_ApprovedKyb_ThrowsKybDomainException()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();
        kyb.StartReview("reviewer@afripay.io");
        kyb.Approve("reviewer@afripay.io");

        var act = () => kyb.SetBusinessInfo("New Name", null, null, null, null);

        act.Should().Throw<KybDomainException>();
    }

    // SetLegalRepresentative

    [Fact]
    public void SetLegalRepresentative_DraftKyb_SetsFields()
    {
        var kyb = CreateDraft();

        kyb.SetLegalRepresentative("Fatoumata Diallo", "fatoumata@shop.sn");

        kyb.LegalRepresentativeName.Should().Be("Fatoumata Diallo");
        kyb.LegalRepresentativeEmail.Should().Be("fatoumata@shop.sn");
    }

    // AddDocument

    [Fact]
    public void AddDocument_DraftKyb_AddsDocumentToCollection()
    {
        var kyb = CreateDraft();

        kyb.AddDocument(DocumentType.BusinessRegistration, "reg.pdf", "storage/reg.pdf", "application/pdf", 102_400);

        kyb.Documents.Should().HaveCount(1);
        kyb.Documents[0].Type.Should().Be(DocumentType.BusinessRegistration);
        kyb.Documents[0].FileName.Should().Be("reg.pdf");
    }

    [Fact]
    public void AddDocument_MultipleDocuments_AllAdded()
    {
        var kyb = CreateDraft();

        kyb.AddDocument(DocumentType.BusinessRegistration, "reg.pdf", "s/reg.pdf", "application/pdf", 1024);
        kyb.AddDocument(DocumentType.OwnerIdentity, "id.jpg", "s/id.jpg", "image/jpeg", 2048);

        kyb.Documents.Should().HaveCount(2);
    }

    // Submit

    [Fact]
    public void Submit_DraftWithAllRequiredDocuments_SetsSubmittedStatus()
    {
        var kyb = CreateReadyToSubmit();

        kyb.Submit();

        kyb.Status.Should().Be(KybStatus.Submitted);
        kyb.SubmittedAt.Should().BeAfter(DateTimeOffset.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Submit_MissingBusinessRegistration_ThrowsKybDomainException()
    {
        var kyb = CreateDraft();
        kyb.AddDocument(DocumentType.OwnerIdentity, "id.jpg", "s/id.jpg", "image/jpeg", 2048);

        var act = () => kyb.Submit();

        act.Should().Throw<KybDomainException>()
            .WithMessage("*Missing required documents*");
    }

    [Fact]
    public void Submit_MissingOwnerIdentity_ThrowsKybDomainException()
    {
        var kyb = CreateDraft();
        kyb.AddDocument(DocumentType.BusinessRegistration, "reg.pdf", "s/reg.pdf", "application/pdf", 1024);

        var act = () => kyb.Submit();

        act.Should().Throw<KybDomainException>()
            .WithMessage("*Missing required documents*");
    }

    [Fact]
    public void Submit_NoLegalName_ThrowsKybDomainException()
    {
        var kyb = KybApplication.Create(MerchantId, "SN");
        // Set business info with empty legal name to reach Draft status
        kyb.SetBusinessInfo("", null, null, null, null);
        kyb.AddDocument(DocumentType.BusinessRegistration, "reg.pdf", "s/reg.pdf", "application/pdf", 1024);
        kyb.AddDocument(DocumentType.OwnerIdentity, "id.jpg", "s/id.jpg", "image/jpeg", 2048);

        var act = () => kyb.Submit();

        act.Should().Throw<KybDomainException>()
            .WithMessage("*Legal name*");
    }

    [Fact]
    public void Submit_NotStartedKyb_ThrowsKybDomainException()
    {
        var kyb = CreateFresh();

        var act = () => kyb.Submit();

        act.Should().Throw<KybDomainException>()
            .WithMessage("*Cannot submit KYB*");
    }

    //StartReview 

    [Fact]
    public void StartReview_SubmittedKyb_SetsUnderReviewStatus()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();

        kyb.StartReview("reviewer@afripay.io");

        kyb.Status.Should().Be(KybStatus.UnderReview);
        kyb.ReviewedBy.Should().Be("reviewer@afripay.io");
    }

    [Fact]
    public void StartReview_NotSubmitted_ThrowsKybDomainException()
    {
        var kyb = CreateDraft();

        var act = () => kyb.StartReview("reviewer@afripay.io");

        act.Should().Throw<KybDomainException>()
            .WithMessage("*Only submitted*");
    }

    //Approve 

    [Fact]
    public void Approve_UnderReviewKyb_SetsApprovedAndExpiryInOneYear()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();
        kyb.StartReview("reviewer@afripay.io");

        var before = DateTimeOffset.UtcNow;
        kyb.Approve("reviewer@afripay.io", "All good");

        kyb.Status.Should().Be(KybStatus.Approved);
        kyb.ApprovedAt.Should().NotBeNull();
        kyb.ExpiresAt.Should().BeCloseTo(before.AddYears(1), TimeSpan.FromSeconds(5));
        kyb.ReviewNote.Should().Be("All good");
    }

    [Fact]
    public void Approve_DraftKyb_ThrowsKybDomainException()
    {
        var kyb = CreateDraft();

        var act = () => kyb.Approve("reviewer@afripay.io");

        act.Should().Throw<KybDomainException>()
            .WithMessage("*under-review*");
    }

    // Reject

    [Fact]
    public void Reject_UnderReviewKyb_SetsRejectedStatus()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();
        kyb.StartReview("reviewer@afripay.io");

        kyb.Reject("reviewer@afripay.io", "Documents illisibles");

        kyb.Status.Should().Be(KybStatus.Rejected);
        kyb.ReviewNote.Should().Be("Documents illisibles");
    }

    [Fact]
    public void Reject_EmptyReason_ThrowsKybDomainException()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();
        kyb.StartReview("reviewer@afripay.io");

        var act = () => kyb.Reject("reviewer@afripay.io", "  ");

        act.Should().Throw<KybDomainException>()
            .WithMessage("*reason is required*");
    }

    [Fact]
    public void Reject_NotUnderReview_ThrowsKybDomainException()
    {
        var kyb = CreateDraft();

        var act = () => kyb.Reject("reviewer@afripay.io", "Not valid");

        act.Should().Throw<KybDomainException>();
    }

    // RequestAdditionalInfo

    [Fact]
    public void RequestAdditionalInfo_UnderReviewKyb_SetsCorrectStatus()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();
        kyb.StartReview("reviewer@afripay.io");

        kyb.RequestAdditionalInfo("reviewer@afripay.io", "Veuillez fournir un extrait Kbis récent.");

        kyb.Status.Should().Be(KybStatus.AdditionalInfoRequired);
        kyb.ReviewNote.Should().Contain("Kbis");
    }

    [Fact]
    public void RequestAdditionalInfo_NotUnderReview_ThrowsKybDomainException()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();

        var act = () => kyb.RequestAdditionalInfo("reviewer@afripay.io", "Need more info");

        act.Should().Throw<KybDomainException>()
            .WithMessage("*during review*");
    }

    // Resubmit after AdditionalInfoRequired 

    [Fact]
    public void Submit_AfterAdditionalInfoRequired_Succeeds()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();
        kyb.StartReview("reviewer@afripay.io");
        kyb.RequestAdditionalInfo("reviewer@afripay.io", "Need more info");

        kyb.Submit();

        kyb.Status.Should().Be(KybStatus.Submitted);
    }

    // Computed 

    [Fact]
    public void IsApproved_AfterApproval_ReturnsTrue()
    {
        var kyb = CreateReadyToSubmit();
        kyb.Submit();
        kyb.StartReview("reviewer@afripay.io");
        kyb.Approve("reviewer@afripay.io");

        kyb.IsApproved.Should().BeTrue();
        kyb.LiveAccessAllowed.Should().BeTrue();
    }

    [Fact]
    public void IsApproved_NotApproved_ReturnsFalse()
    {
        var kyb = CreateDraft();

        kyb.IsApproved.Should().BeFalse();
        kyb.LiveAccessAllowed.Should().BeFalse();
    }
}