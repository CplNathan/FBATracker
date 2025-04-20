// <copyright file="ItemEligibilityBatchWorker.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Services.Batch;

using FBATrackerReact.Models.Amazon;
using FBATrackerReact.Server.Constants;
using FBATrackerReact.Server.Models.ConcurrentQueue;
using FikaAmazonAPI;
using FikaAmazonAPI.Parameter.FbaInboundEligibility;
using FikaAmazonAPI.Parameter.Restrictions;
using static FikaAmazonAPI.AmazonSpApiSDK.Models.FbaInbound.ItemEligibilityPreview;

public sealed class ItemEligibilityBatchWorker(AmazonConnection amazonConnection, ILogger<IBatchWorker<AmazonEligibility>> logger) : BaseBatchWorker<AmazonEligibility>(logger, (requestItems, token) => GetEligibilityImplementation(logger, amazonConnection, requestItems, token), TimeSpan.FromSeconds(1), 1)
{
    private const string ReasonCode = "APPROVAL_REQUIRED";

    private static async Task<List<BatchQueueItem<AmazonEligibility>>> GetEligibilityImplementation(ILogger<IBatchWorker<AmazonEligibility>> logger, AmazonConnection amazonConnection, IEnumerable<BatchQueueItem<AmazonEligibility>> referenceList, CancellationToken token)
    {
        logger.LogTrace("Searching eligibility for {items}", referenceList.Select(x => x.ItemReference));

        FikaAmazonAPI.Utils.MarketPlace currentMarketplace = AppConstants.DefaultMarketPlace;

        BatchQueueItem<AmazonEligibility> reference = referenceList.First();
        Task<FikaAmazonAPI.AmazonSpApiSDK.Models.FbaInbound.ItemEligibilityPreview> fbaEligibility = amazonConnection.FbaInboundEligibility.GetItemEligibilityPreviewAsync(
            new ParameterGetItemEligibilityPreview
            {
                asin = reference.ItemReference,
                marketplaceIds = [currentMarketplace.ID],
                program = ProgramEnum.INBOUND,
            },
            token);

        Task<FikaAmazonAPI.AmazonSpApiSDK.Models.Restrictions.RestrictionList> sellingRestrictions = amazonConnection.Restrictions.GetListingsRestrictionsAsync(
            new ParameterGetListingsRestrictions
            {
                asin = reference.ItemReference,
                marketplaceIds = [currentMarketplace.ID],
                sellerId = amazonConnection.GetCurrentSellerID,
            },
            token);

        await Task.WhenAll(fbaEligibility, sellingRestrictions);

        reference.ItemData = new AmazonEligibility
        {
            IsGateLocked = sellingRestrictions.Result.Restrictions.Any(x => x.Reasons.Any(y => y.ReasonCode == ReasonCode)),
            IsRestricted = !fbaEligibility.Result.IsEligibleForProgram ?? false,
            RestrictedReason = fbaEligibility.Result.IneligibilityReasonList?.Select(GetEligibilityString).ToList(),
            GatedOnwardUrl = sellingRestrictions.Result.Restrictions.FirstOrDefault(x => x.Reasons.Any(y => y.ReasonCode == ReasonCode))?.Reasons.First(x => x.ReasonCode == ReasonCode).Links.First()?.Resource,
        };

        return [reference];
    }

    private static string GetEligibilityString(IneligibilityReasonListEnum reason)
    {
        return reason switch
        {
            IneligibilityReasonListEnum.FBAINB0004 => "Missing package dimensions. This product is missing necessary information; dimensions need to be provided in the manufacturer's original packaging.",
            IneligibilityReasonListEnum.FBAINB0006 => "The SKU for this product is unknown or cannot be found.",
            IneligibilityReasonListEnum.FBAINB0007 => "Product Under Dangerous Goods (Hazmat) Review. We do not have enough information to determine what the product is or comes with to enable us to complete our dangerous goods review. Until you provide the necessary information, the products will not be available for sale and you will not be able to send more units to Amazon fulfillment centers. You will need to add more details to the product listings, such as a clear title, bullet points, description, and image. The review process takes 4 business days.",
            IneligibilityReasonListEnum.FBAINB0008 => "Product Under Dangerous Goods (Hazmat) Review. We require detailed battery information to correctly classify the product, and until you provide the necessary information, the products will not be available for sale and you will not be able to send more units to Amazon fulfillment centers. Download an exemption sheet for battery and battery-powered products available in multiple languages in \"Upload dangerous goods documents: safety data sheet (SDS) or exemption sheet\" in Seller Central and follow instructions to submit it through the same page. The review process takes 4 business days.",
            IneligibilityReasonListEnum.FBAINB0009 => "Product Under Dangerous Goods (Hazmat) Review. We do not have enough dangerous goods information to correctly classify the product and until you provide the necessary information, the products will not be available for sale and you will not be able to send more units to Amazon fulfillment centers. Please provide a Safety Data Sheet (SDS) through \"Upload dangerous goods documents: safety data sheet (SDS) or exemption sheet\" in Seller Central, and make sure the SDS complies with all the requirements. The review process takes 4 business days.",
            IneligibilityReasonListEnum.FBAINB0010 => "Product Under Dangerous Goods (Hazmat) Review. The dangerous goods information is mismatched and so the product cannot be correctly classified. Until you provide the necessary information, the products will not be available for sale and you will not be able to send more units to Amazon fulfillment centers. Please provide compliant documents through \"Upload dangerous goods documents: safety data sheet (SDS) or exemption sheet\" in Seller Central, and make sure it complies with all the requirements. The review process takes 4 business days, the product will remain unfulfillable until review process is complete.",
            IneligibilityReasonListEnum.FBAINB0011 => "Product Under Dangerous Goods (Hazmat) Review. We have incomplete, inaccurate or conflicting dangerous goods information and cannot correctly classify the product. Until you provide the necessary information, the products will not be available for sale and you will not be able to send more units to Amazon fulfillment centers. Please provide compliant documents through \"Upload dangerous goods documents: safety data sheet (SDS) or exemption sheet\" in Seller Central, and make sure it complies with all the requirements. The review process takes 4 business days and the product will remain unfulfillable until the review process is complete.",
            IneligibilityReasonListEnum.FBAINB0012 => "Product Under Dangerous Goods (Hazmat) Review. We have determined there is conflicting product information (title, bullet points, images, or product description) within the product detail pages or with other offers for the product. Until the conflicting information is corrected, the products will not be available for sale and you will not be able to send more units to Amazon fulfillment centers. We need you to confirm the information on the product detail page The review process takes 4 business days.",
            IneligibilityReasonListEnum.FBAINB0013 => "Product Under Dangerous Goods (Hazmat) Review. Additional information is required in order to complete the Hazmat review process.",
            IneligibilityReasonListEnum.FBAINB0014 => "Product Under Dangerous Goods (Hazmat) Review. The product has been identified as possible dangerous goods. The review process generally takes 4 - 7 business days and until the review process is complete the product is unfulfillable and cannot be received at Amazon fulfilment centers or ordered by customers. For more information about dangerous goods please see \"Dangerous goods identification guide (hazmat)\"\" help page in Seller Central.",
            IneligibilityReasonListEnum.FBAINB0015 => "Dangerous goods (Hazmat). The product is regulated as unfulfillable and not eligible for sale with Amazon. We ask that you refrain from sending additional units in new shipments. We will need to dispose of your dangerous goods inventory in accordance with the terms of the Amazon Business Services Agreement. If you have questions or concerns, please contact Seller Support within five business days of this notice. For more information about dangerous goods please see “Dangerous goods identification guide (hazmat)” help page in Seller Central.",
            IneligibilityReasonListEnum.FBAINB0016 => "Dangerous goods (Hazmat). The product is regulated as a fulfillable dangerous good (Hazmat). You may need to be in the FBA dangerous good (Hazmat) program to be able to sell your product. For more information on the FBA dangerous good (Hazmat) program please contact Seller Support. For more information about dangerous goods please see the \"Dangerous goods identification guide (hazmat)\" help page in Seller Central.",
            IneligibilityReasonListEnum.FBAINB0017 => "This product does not exist in the destination marketplace catalog. The necessary product information will need to be provided before it can be inbounded.",
            IneligibilityReasonListEnum.FBAINB0018 => "Product missing category. This product must have a category specified before it can be sent to Amazon.",
            IneligibilityReasonListEnum.FBAINB0019 => "This product must have a title before it can be sent to Amazon.",
            IneligibilityReasonListEnum.FBAINB0034 => "Product cannot be stickerless, commingled. This product must be removed. You can send in new inventory by creating a new listing for this product that requires product labels.",
            IneligibilityReasonListEnum.FBAINB0035 => "Expiration-dated/lot-controlled product needs to be labeled. This product requires labeling to be received at our fulfillment centers.",
            IneligibilityReasonListEnum.FBAINB0036 => "Expiration-dated or lot-controlled product needs to be commingled. This product cannot be shipped to Amazon without being commingled. This error condition cannot be corrected from here. This product must be removed.",
            IneligibilityReasonListEnum.FBAINB0037 => "This product is not eligible to be shipped to our fulfillment center. You do not have all the required tax documents. If you have already filed documents please wait up to 48 hours for the data to propagate.",
            IneligibilityReasonListEnum.FBAINB0038 => "Parent ASIN cannot be fulfilled by Amazon. You can send this product by creating a listing against the child ASIN.",
            IneligibilityReasonListEnum.FBAINB0050 => "There is currently no fulfillment center in the destination country capable of receiving this product. Please delete this product from the shipment or contact Seller Support if you believe this is an error.",
            IneligibilityReasonListEnum.FBAINB0051 => "This product has been blocked by FBA and cannot currently be sent to Amazon for fulfillment.",
            IneligibilityReasonListEnum.FBAINB0053 => "Product is not eligible in the destination marketplace. This product is not eligible either because the required shipping option is not available or because the product is too large or too heavy.",
            IneligibilityReasonListEnum.FBAINB0055 => "Product unfulfillable due to media region restrictions. This product has a region code restricted for this marketplace. This product must be removed.",
            IneligibilityReasonListEnum.FBAINB0056 => "Product is ineligible for inbound. Used non-media goods cannot be shipped to Amazon.",
            IneligibilityReasonListEnum.FBAINB0059 => "Unknown Exception. This product must be removed at this time.",
            IneligibilityReasonListEnum.FBAINB0065 => "Product cannot be stickerless, commingled. This product must be removed. You can send in new inventory by creating a new listing for this product that requires product labels.",
            IneligibilityReasonListEnum.FBAINB0066 => "Unknown Exception. This product must be removed at this time.",
            IneligibilityReasonListEnum.FBAINB0067 => "Product ineligible for freight shipping. This item is ineligible for freight shipping with our Global Shipping Service. This item must be removed.",
            IneligibilityReasonListEnum.FBAINB0068 => "Account not configured for expiration-dated or lot-controlled products. Please contact TAM if you would like to configure your account to handle expiration-dated or lot-controlled inventory. Once configured, you will be able to send in this product.",
            IneligibilityReasonListEnum.FBAINB0095 => "The barcode (UPC/EAN/JAN/ISBN) for this product is associated with more than one product in our fulfillment system. This product must be removed. You can send in new inventory by creating a new listing for this product that requires product labels.",
            IneligibilityReasonListEnum.FBAINB0097 => "Fully regulated dangerous good.",
            IneligibilityReasonListEnum.FBAINB0098 => "Merchant is not authorized to send item to destination marketplace.",
            IneligibilityReasonListEnum.FBAINB0099 => "Seller account previously terminated.",
            IneligibilityReasonListEnum.FBAINB0100 => "You do not have the required tax information to send inventory to fulfillment centers in Mexico.",
            IneligibilityReasonListEnum.FBAINB0103 => "This is an expiration-dated/lot-controlled product that cannot be handled at this time.",
            IneligibilityReasonListEnum.FBAINB0104 => "Item Requires Manufacturer Barcode. Only NEW products can be stored in our fulfillment centers without product labels.",
            IneligibilityReasonListEnum.UNKNOWNINBERRORCODE or _ => "Unknown",
        };
    }
}