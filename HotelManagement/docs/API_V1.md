# REST API v1

Base path: `/api/v1`. The API uses JSON, ISO-8601 date/time values, and the same hotel database and booking service as the MVC screens.

## Authentication

All current endpoints require an authenticated `Admin` or `NhanVien` account. Sign in through the existing Identity login and send the application authentication cookie with same-origin requests. API business operations do not read or depend on ASP.NET Session values. Session stores only UI context (`UserId`, `UserEmail`, `UserRole`, `LoginAtUtc`); Identity remains the source of authentication and authorization.

For mobile or third-party clients, add a bearer-token flow before exposing the API across origins. Do not use the Session ID as an API credential.

## Endpoints

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/rooms?page=1&pageSize=20&status=Trong` | List rooms, optionally filter by current status |
| GET | `/rooms/{id}` | Room details |
| GET | `/items?keyword=nuoc&type=SanPham&active=true&page=1&pageSize=20` | Search services/products |
| GET | `/items/{id}` | Service/product details |
| GET | `/bookings?status=DangLuuTru&from=2026-11-01&to=2026-11-30&page=1&pageSize=20` | List bookings filtered by status and planned arrival date |
| GET | `/bookings/{id}` | Booking, guest, room, and actual stay details |
| POST | `/bookings` | Create a reservation using the existing availability validation |
| POST | `/bookings/{id}/rooms/{roomId}/check-in` | Check in one room under a multi-room booking |
| POST | `/bookings/{id}/rooms/{roomId}/check-out` | Check out one room; the booking completes after all rooms check out |

`pageSize` accepts 10, 20, or 50. Collection responses include `items`, `page`, `pageSize`, `totalItems`, and `totalPages`.

## Create booking

```http
POST /api/v1/bookings
Content-Type: application/json
```

```json
{
  "guestId": 12,
  "checkIn": "2026-11-07T14:00:00+07:00",
  "checkOut": "2026-11-09T12:00:00+07:00",
  "deposit": 500000,
  "roomIds": [3, 4],
  "note": "Hai phòng cùng tầng nếu có thể"
}
```

The API responds `201 Created` with a `Location` header for the new booking. Room availability, dates, guest existence, and deposit rules are validated by `DatPhongService`, the same service used by the MVC workflow. Business-rule conflicts return `409 Conflict` as `ProblemDetails`; invalid request fields return `400`.

## Collection response example

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

## Response and state rules

- IDs are numeric database IDs; booking codes use `DP` plus a zero-padded ID.
- Room status and booking status use the existing database values. Clients should display a friendly label rather than inventing new state values.
- `404 Not Found` is used when a room, item, or booking does not exist.
- Check-in/out operations return `409 Conflict` when the requested room transition is invalid.
- POST operations must be sent over HTTPS in deployment. Keep cookie-authenticated requests same-origin and retain the ASP.NET antiforgery protection for MVC forms.

## Next API growth

Add inventory receipt creation, payment recording, and customer-facing booking endpoints only after defining their DTOs and authorization rules. Keep controllers thin and place booking, payment, and stock invariants in application services so MVC and API calls cannot diverge.
