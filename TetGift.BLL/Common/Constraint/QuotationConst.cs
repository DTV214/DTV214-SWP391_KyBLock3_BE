namespace TetGift.BLL.Common.Enums
{
    public static class QuotationStatus
    {
        public const string DRAFT = "DRAFT";
        public const string SUBMITTED = "SUBMITTED";
        public const string STAFF_REVIEWING = "STAFF_REVIEWING";
        public const string WAITING_ADMIN = "WAITING_ADMIN";
        public const string ADMIN_REJECTED = "ADMIN_REJECTED";
        public const string WAITING_CUSTOMER = "WAITING_CUSTOMER";
        public const string CUSTOMER_REJECTED = "CUSTOMER_REJECTED";
        public const string CUSTOMER_ACCEPTED = "CUSTOMER_ACCEPTED";
        public const string CONVERTED_TO_ORDER = "CONVERTED_TO_ORDER";
        public const string CANCELLED = "CANCELLED";
    }

    public static class QuotationType
    {
        public const string MANUAL = "MANUAL";
        public const string BUDGET_RECOMMEND = "BUDGET_RECOMMEND";
    }

    public static class QuotationRole
    {
        public const string CUSTOMER = "CUSTOMER";
        public const string STAFF = "STAFF";
        public const string ADMIN = "ADMIN";
        public const string SYSTEM = "SYSTEM";
    }

    public static class QuotationAction
    {
        public const string SUBMIT = "SUBMIT";
        public const string START_REVIEW = "START_REVIEW";
        public const string STAFF_PROPOSE = "STAFF_PROPOSE";
        public const string SEND_ADMIN = "SEND_ADMIN";
        public const string ADMIN_APPROVE = "ADMIN_APPROVE";
        public const string ADMIN_REJECT = "ADMIN_REJECT";
        public const string SEND_CUSTOMER = "SEND_CUSTOMER";
        public const string CUSTOMER_ACCEPT = "CUSTOMER_ACCEPT";
        public const string CUSTOMER_REJECT = "CUSTOMER_REJECT";
        public const string CONVERT_ORDER = "CONVERT_ORDER";
        public const string NOTE = "NOTE";
        public const string RECOMMEND_PREVIEW = "RECOMMEND_PREVIEW";
        public const string RECOMMEND_CONFIRM = "RECOMMEND_CONFIRM";
    }
}
