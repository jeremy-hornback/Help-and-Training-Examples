using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;

namespace PhoneRepairShop
{
    public class RSSVPaymentPlanInq : PXGraph<RSSVPaymentPlanInq>
    {
        ...

        protected virtual void _(Events.FieldSelecting<RSSVWorkOrderToPay,
            RSSVWorkOrderToPay.percentPaid> e)
        {
            if (e.Row == null) return;
            if (e.Row.OrderTotal == 0) return;

            RSSVWorkOrderToPay order = e.Row;
            var invoices = SelectFrom<ARInvoice>.
                Where<ARInvoice.refNbr.IsEqual<@P.AsString>>.View.Select(
                this, order.InvoiceNbr);
            if (invoices.Count == 0)
                return;
            ARInvoice first = invoices[0];
            e.ReturnValue = (order.OrderTotal - first.CuryDocBal) /
                order.OrderTotal * 100;
        }
    }
}