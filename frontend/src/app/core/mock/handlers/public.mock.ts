import { HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { SubscriptionInquiryRequest, SubscriptionInquiryResponse } from '../../models/public.models';
import { ProblemDetails } from '../../models/api-response.models';

const EGYPTIAN_PHONE_REGEX = /^01[0125][0-9]{8}$/;

export function handlePublicInquiry(req: HttpRequest<unknown>): Observable<HttpResponse<SubscriptionInquiryResponse>> {
  const body = req.body as SubscriptionInquiryRequest;

  if (!body || !body.phone || !EGYPTIAN_PHONE_REGEX.test(body.phone)) {
    const errorDetails: ProblemDetails = {
      type: 'https://tahdeera.app/errors/validation-failed',
      title: 'Validation Failed',
      status: 422,
      errorCode: 'VALIDATION_FAILED',
      detail: 'فشل التحقق من صحة البيانات المدخلة',
      instance: req.url,
      traceId: `00-${Date.now()}-01`,
      errors: {
        phone: ['رقم الهاتف غير صحيح (يجب أن يبدأ بـ 010 أو 011 أو 012 أو 015 ويحتوي على 11 رقم)']
      }
    };

    return throwError(() => new HttpErrorResponse({
      status: 422,
      statusText: 'Unprocessable Content',
      error: errorDetails,
      url: req.url
    }));
  }

  const response: SubscriptionInquiryResponse = {
    success: true,
    inquiryId: 'inq-981',
    message: 'تم استلام طلبكم بنجاح وسيتواصل معكم فريق الدعم خلال ساعتين.'
  };

  return of(new HttpResponse({
    status: 201,
    body: response
  }));
}
