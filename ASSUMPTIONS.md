# Assumptions and implementation decisions

The original company assignment PDF was not present in the workspace during this review. The implementation follows the supplied project requirements; the routes and DTO shapes below are documented implementation choices where no separate authoritative API contract was available.

## Requirements represented by the supplied brief

- Each class may have multiple shifts, and seats are allocated per class/shift combination.
- A valid application holds one seat in Pending status for 600 seconds by default.
- A student is identified by a unique 17-digit Birth Registration Number and may have at most one active hold.
- Payment is simulated; successful confirmation changes Pending to Confirmed.
- Expired pending holds are released by a backend background service.

## Technical decisions

- Admin application management is available without login for the assignment demo because no admin authentication mechanism was specified. This must not be exposed publicly without adding access control.
- The student ID returned at application creation is stored in browser local storage only to make the student's lookup convenient. PostgreSQL remains the source of truth.
- Seat totals in the class list are the sum of its configured class/shift capacities.
- Payment amount is zero because no actual fee or payment gateway was specified.
- The API returns JSON enum names and HTTP 409 for business conflicts such as unavailable seats, duplicate active holds, and expired payments.
- Docker Compose supplies local-development defaults. Use private credentials for any non-local deployment.
