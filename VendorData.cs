using Newtonsoft.Json;
#nullable disable

//MYOB Advanced vendor data fields. This is a public API.

namespace MyobAdvancedFunction.Vendors
{
    public class VendorData
    {
        public string id { get; set; }
        public int rowNumber { get; set; }
        public Note note { get; set; }
        public AccountRef AccountRef { get; set; }
        public APAccount APAccount { get; set; }
        public APSubaccount APSubaccount { get; set; }
        public CashAccount CashAccount { get; set; }
        public CreatedDateTime CreatedDateTime { get; set; }
        public CurrencyID CurrencyID { get; set; }
        public CurrencyRateType CurrencyRateType { get; set; }
        public EnableCurrencyOverride EnableCurrencyOverride { get; set; }
        public EnableRateOverride EnableRateOverride { get; set; }
        public FOBPoint FOBPoint { get; set; }
        public LandedCostVendor LandedCostVendor { get; set; }
        public LastModifiedDateTime LastModifiedDateTime { get; set; }
        public LeadTimedays LeadTimedays { get; set; }
        public LocationName LocationName { get; set; }
        public MaxReceipt MaxReceipt { get; set; }
        public MinReceipt MinReceipt { get; set; }
        public ParentAccount ParentAccount { get; set; }
        public PaymentBy PaymentBy { get; set; }
        public PaymentLeadTimedays PaymentLeadTimedays { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaySeparately PaySeparately { get; set; }
        public PrintOrders PrintOrders { get; set; }
        public ReceiptAction ReceiptAction { get; set; }
        public ReceivingBranch ReceivingBranch { get; set; }
        public RemittanceAddressOverride RemittanceAddressOverride { get; set; }
        public RemittanceContactOverride RemittanceContactOverride { get; set; }
        public SendOrdersbyEmail SendOrdersbyEmail { get; set; }
        public ShippingAddressOverride ShippingAddressOverride { get; set; }
        public ShippingContactOverride ShippingContactOverride { get; set; }
        public ShippingTerms ShippingTerms { get; set; }
        public ShipVia ShipVia { get; set; }
        public Status Status { get; set; }
        public TaxCalculationMode TaxCalculationMode { get; set; }
        public TaxRegistrationID TaxRegistrationID { get; set; }
        public TaxZone TaxZone { get; set; }
        public Terms Terms { get; set; }
        public ThresholdReceipt ThresholdReceipt { get; set; }
        public VendorClass VendorClass { get; set; }
        public VendorID VendorID { get; set; }
        public VendorIsLaborUnion VendorIsLaborUnion { get; set; }
        public VendorIsTaxAgency VendorIsTaxAgency { get; set; }
        public VendorName VendorName { get; set; }
        public Custom custom { get; set; }
        //public Links _links { get; set; }
    }


    public class AccountRef
    {
        public string value { get; set; }
    }

    public class APAccount
    {
        public string value { get; set; }
    }

    public class APSubaccount
    {
        public string value { get; set; }
    }

    public class CashAccount
    {
        public string value { get; set; }
    }

    public class CreatedDateTime
    {
        public DateTime value { get; set; }
    }

    public class CurrencyID
    {
        public string value { get; set; }
    }

    public class CurrencyRateType
    {
        public string value { get; set; }
    }

    public class Custom
    {
        public string value { get; set; }
    }

    public class EnableCurrencyOverride
    {
        public bool value { get; set; }
    }

    public class EnableRateOverride
    {
        public bool value { get; set; }
    }

    public class FOBPoint
    {
        public string value { get; set; }
    }

    public class LandedCostVendor
    {
        public bool value { get; set; }
    }

    public class LastModifiedDateTime
    {
        public DateTime value { get; set; }
    }

    public class LeadTimedays
    {
        public int value { get; set; }
    }

    public class Links
    {
        public string self { get; set; }

        [JsonProperty("files:put")]
        public string filesput { get; set; }
    }

    public class LocationName
    {
        public string value { get; set; }
    }

    public class MaxReceipt
    {
        public double value { get; set; }
    }

    public class MinReceipt
    {
        public double value { get; set; }
    }

    public class Note
    {
        public string value { get; set; }
    }

    public class ParentAccount
    {
        public string value { get; set; }
    }

    public class PaymentBy
    {
        public string value { get; set; }
    }

    public class PaymentLeadTimedays
    {
        public int value { get; set; }
    }

    public class PaymentMethod
    {
        public string value { get; set; }
    }

    public class PaySeparately
    {
        public bool value { get; set; }
    }

    public class PrintOrders
    {
        public bool value { get; set; }
    }

    public class ReceiptAction
    {
        public string value { get; set; }
    }

    public class ReceivingBranch
    {
        public string value { get; set; }
    }

    public class RemittanceAddressOverride
    {
        public string value { get; set; }
    }

    public class RemittanceContactOverride
    {
        public string value { get; set; }
    }

    public class SendOrdersbyEmail
    {
        public bool value { get; set; }
    }

    public class ShippingAddressOverride
    {
        public string value { get; set; }
    }

    public class ShippingContactOverride
    {
        public string value { get; set; }
    }

    public class ShippingTerms
    {
        public string value { get; set; }
    }

    public class ShipVia
    {
        public string value { get; set; }
    }

    public class Status
    {
        public string value { get; set; }
    }

    public class TaxCalculationMode
    {
        public string value { get; set; }
    }

    public class TaxRegistrationID
    {
        public string value { get; set; }
    }

    public class TaxZone
    {
        public string value { get; set; }
    }

    public class Terms
    {
        public string value { get; set; }
    }

    public class ThresholdReceipt
    {
        public double value { get; set; }
    }

    public class VendorClass
    {
        public string value { get; set; }
    }

    public class VendorID
    {
        public string value { get; set; }
    }

    public class VendorIsLaborUnion
    {
        public bool value { get; set; }
    }

    public class VendorIsTaxAgency
    {
        public bool value { get; set; }
    }

    public class VendorName
    {
        public string value { get; set; }
    }
}
