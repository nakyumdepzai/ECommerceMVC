using ECommerceMVC.Models;

namespace ECommerceMVC.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model);
        //VnPaymentRequestModel PaymentExecute(IQueryCollection collections);
        VnPaymentResponseModel PaymentExecute(IQueryCollection collections);


    }
}
