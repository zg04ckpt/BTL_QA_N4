# Refactor Progress (Phase 0 -> Phase 3)

## Phase 0 - Baseline Lock

- Baseline commit selected for frontend behavior: `4f54e73` (before large API expansion and feature spread).
- Current working branch contains broad additions after baseline:
  - backend feature expansion (`favorite`, `report`, `review` management changes),
  - frontend integration drift and mixed data flow.

### Baseline alignment rule

- Keep core baseline user app flow.
- Only preserve post-baseline additions for:
  - user management,
  - favorite restaurants,
  - notifications.

## Phase 1 - Scope Cut (implemented)

### Kept

- User main tabs: home/discovery/bookmark/profile.
- Admin: user management + post notifications.
- Favorite support (bookmark tab and favorite APIs).

### Removed from active UI scope

- User profile entries for:
  - review history,
  - order history,
  - report history,
  - restaurant manager entry.
- Admin entry points for:
  - restaurant management,
  - review management.
- App-level provider wiring for:
  - order provider,
  - report provider.

## Files updated in Phase 1

- `frontend/lib/main.dart`
- `frontend/lib/view/my_profile/my_profile_view.dart`
- `frontend/lib/view/admin/home_admin/home_admin_view.dart`

## Phase 2 - API Contract Adapter (implemented)

- Added `frontend/lib/network/api_mapper.dart` as a normalization layer for:
  - numeric parsing (`int`, `double`),
  - string defaults,
  - list parsing,
  - media URL resolution.
- Integrated adapter into:
  - `frontend/lib/data/models/user_data.dart`,
  - `frontend/lib/data/models/restaurant.dart`.

## Phase 3 - Provider State Cleanup (implemented, targeted)

- `frontend/lib/services/restaurant_provider.dart`
  - Added favorites state:
    - `favoriteRestaurants`,
    - `isLoadingFavorites`,
    - `favoriteLoadError`.
  - Unified favorites load/toggle behavior against API response.
- `frontend/lib/view/bookmark/bookmark_view.dart`
  - Switched source of truth from local SQLite helper to `RestaurantProvider` favorite state.
  - Added loading/error/empty rendering states.

## Phase 4 - UI Guard Hardening (partial)

- `frontend/lib/view/admin/user_management/user_management_view.dart`
  - Removed invalid null-aware accesses for non-null `UserData` fields.

## Remaining

- Phase 4 full sweep for all legacy screens not in active scope.
- Phase 5 regression checklist execution on device.
