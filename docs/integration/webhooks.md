# HIS/EMR Webhooks

## Event

Supported event: `clinical.calculation.completed`.

Clinical audit và outbox event được thêm trong cùng một database `SaveChanges`. Request không chờ webhook delivery.

## Subscription requirements

- Endpoint phải là absolute HTTPS URL.
- Secret tối thiểu 32 ký tự và được bảo vệ bằng ASP.NET Core Data Protection trước khi lưu.
- Production mặc định chặn loopback, link-local, private IPv4/IPv6 và DNS resolve vào private network.
- Chỉ bật `Webhooks:AllowPrivateNetworks=true` cho on-prem deployment đã kiểm soát network.
- `Webhooks:AllowLoopback=true` chỉ dành cho automated integration test; cấm dùng production.
- HTTP redirects bị tắt.

## Delivery headers

```http
X-BiliTool-Event-Id: <outbox-event-guid>
X-BiliTool-Event-Type: clinical.calculation.completed
X-BiliTool-Timestamp: <unix-seconds>
X-BiliTool-Signature: v1=<lowercase-hex-hmac-sha256>
```

Signature input:

```text
<timestamp>.<exact-request-body>
```

Consumer phải:

1. Từ chối timestamp ngoài replay window đã thống nhất.
2. Tính HMAC-SHA256 bằng secret riêng của subscription.
3. So sánh signature constant-time.
4. Deduplicate bằng `X-BiliTool-Event-Id`.
5. Chỉ trả 2xx sau khi event được lưu bền vững.

## Retry and dead letter

- Timeout delivery: 10 giây; connect timeout: 5 giây.
- Lease processing: 2 phút, reclaim được nếu worker chết.
- Backoff: 30s, 60s, 120s, 240s, 480s, 960s, 1920s, tối đa 3600s.
- Sau 8 lần thất bại, event chuyển `DeadLetter`.
- Disabled subscription chuyển event đang xử lý sang `DeadLetter`.
- `LastError` bị giới hạn 2000 ký tự; không lưu response body của đối tác.

## Operations

- `POST /admin/operations/outbox/{eventId}/replay` đưa event `DeadLetter` về queue khi subscription còn active.
- Replay ghi admin audit action `his.outbox.dead_letter.replay`.
- Integration suite chạy HTTPS Kestrel thật và xác minh exact body, event headers, timestamp cùng HMAC signature.
