# Learnix — Feature Specification

---

## 1. Authentication & Authorization

### Registration / Login
- Email + password registration
- Login via email + password
- OAuth 2.0 — Google (register and login)
- JWT access token + refresh token rotation
- HttpOnly cookie for refresh token

### Email verification
- Confirmation email sent after registration
- Account is restricted until email is confirmed
- Resend confirmation email option

### Password reset
- "Forgot password" flow — sends reset link to email
- Token-based reset link with expiry (e.g. 1 hour)
- Invalidate token after use

### Roles
| Role | Description |
|---|---|
| `Student` | Default role on registration |
| `Instructor` | Granted after submitting an application approved by Admin |
| `Admin` | Full platform access |

### Instructor application flow
- Any Student can submit an "Become an Instructor" application
- Application includes: motivation text, optionally a link (portfolio/LinkedIn)
- Application statuses: `Pending` / `Approved` / `Rejected`
- Admin sees list of pending applications in the admin panel
- On approval → user's role is upgraded to `Instructor`, notification sent
- On rejection → notification sent with optional reason
- A user can have only one active application at a time

---

## 2. Student Features

### Course browsing
- Browse all published courses
- Search by title / keyword
- Filter by: category, price (free/paid), rating, duration
- Pagination on course listing
- Sort by: newest, most popular, highest rated

### Course enrollment
- Enroll in free courses instantly
- Enroll in paid courses after mock payment
- View enrolled courses in personal dashboard

### Lesson viewing
- View video lessons (with player)
- View post lessons (with markdown rendering)
- Mark lesson as completed
- Like individual lessons

### Tests
- Take tests attached to lessons
- See score and correct answers after submission
- Retry based on attempt settings (unlimited or limited + cooldown)

### Profile
- Edit display name, avatar, bio
- Set learning preferences (categories of interest) for recommendations
- View earned achievements and certificates

### Achievements
- Awarded automatically based on actions
- List of achievements (see section 6)

### AI Assistant
- Chat widget available on all pages
- Sends question to server → processed by AI (Claude API)
- Conversation history saved per user session (MongoDB)
- Streaming response (SSE)

### Recommendations
- Homepage shows relevant and popular courses
- Based on: enrollment history, liked lessons, set preferences
- *(Low priority — implement last)*

---

## 3. Instructor Features

### Course management
- Create / edit / delete courses
- Set course: title, description, category, price, cover image, tags
- Course status: `Draft` / `Published` / `Archived`

### Course structure
- Courses are divided into **Sections** (modules)
- Sections contain **Lessons**
- Lessons can be reordered via drag-and-drop

### Lesson types

#### Video lesson
- Title
- Description (markdown supported)
- Video file attachment (uploaded to Azure Blob)

#### Post lesson
- Title (always displayed as bold)
- Rich text description with markdown support:
  - Bold, italic, underline
  - Ordered and unordered lists
  - (No code blocks or embedded images required at v1)

#### Assignment (Test)
- Title and description
- One or more questions (see section 5)
- Attempt settings: unlimited or limited (N attempts, cooldown between attempts)

### Instructor dashboard
- List of own courses with enrollment counts
- Basic stats: total students, completion rate per course

---

## 4. Admin Features

- View and manage all users (search, filter by role, ban/unban)
- Assign or revoke Instructor role
- View all courses (published and drafts)
- Unpublish or delete any course
- View platform-level logs (errors, key actions)
- View mock payment history

---

## 5. Tests (Quiz System)

### Question types

| Type | Description |
|---|---|
| Single choice | One correct answer from options |
| Multiple choice | One or more correct answers |
| Text input | User types an answer |

### Text input options
- **Ignore case** — comparison is case-insensitive
- **Fuzzy match** — allow 1 character error if answer length > 1 (Levenshtein distance = 1)
- Exact match (default)

### Attempt settings (per test)
- **Unlimited attempts** — retake anytime
- **Limited attempts** — N attempts allowed; after exhausting, user must wait for cooldown period (e.g. 24h) before retrying

### Scoring
- Score shown as percentage after submission
- Correct answers revealed after submission
- Passing threshold configurable per test (e.g. 70%)

---

## 6. Achievements System

Achievements are awarded automatically when conditions are met.
Each achievement has a **name**, **description**, **icon**, and **XP value**.

### Lesson-based
| Achievement | Condition |
|---|---|
| First Step | Complete first lesson |
| Getting Started | Complete 5 lessons |
| On a Roll | Complete 15 lessons |
| Knowledge Seeker | Complete 50 lessons |

### Course-based
| Achievement | Condition |
|---|---|
| Course Graduate | Complete 1 course |
| Triple Crown | Complete 3 courses |
| Scholar | Complete 5 courses |

### Test-based
| Achievement | Condition |
|---|---|
| Test Taker | Complete first test |
| Quiz Enthusiast | Complete 10 tests |
| Perfect Score | Get 100% on any test |
| Speed Runner | Complete a test with 20+ questions in under 4 minutes |

### Social / other
| Achievement | Condition |
|---|---|
| Fan | Like 10 lessons |
| Profile Complete | Fill in bio and set avatar |
| Early Adopter | Register during launch period *(optional, manual grant)* |

> Note: Achievements are separate records, not "tiers of one achievement".
> This makes it easier to extend and display individually.

---

## 7. Chat (Student ↔ Instructor)

- Students can send messages to the instructor of an enrolled course
- Instructor can reply to students
- Simple 1-on-1 messaging per course context (not a global chat)
- Real-time or near-real-time (polling or SignalR)
- Message history stored in MongoDB

---

## 8. Certificates

- Generated automatically when a student completes all lessons in a course
- Certificate includes: student name, course name, completion date, unique ID
- Available for download as PDF
- Shareable via unique URL

---

## 9. Mock Payment

- Stripe Test Mode (real Stripe API, test card numbers)
- Checkout flow: course page → payment page → confirmation
- Payment creates an `Enrollment` record on success
- Payment status: `Pending` / `Completed` / `Failed`
- Payment history visible to student and admin

---

## 10. Notifications *(suggested addition)*

> Currently missing from spec — worth adding.

- In-app notification bell (top navbar)
- Triggered by: new message from instructor, achievement earned, enrollment confirmed, certificate ready
- Mark as read / mark all as read
- Stored in PostgreSQL, max last 50 per user

---

## 11. Course Ratings & Reviews *(suggested addition)*

> Currently missing — common expectation for an LMS portfolio project.

- Student can leave a rating (1–5 stars) and text review after completing a course
- Average rating shown on course card and detail page
- Reviews stored in MongoDB (flexible schema, no joins needed)
- Instructor cannot review own courses

---

## 12. Security

- Rate limiting on auth endpoints (login, register, password reset)
- JWT with short expiry (15 min) + refresh token rotation
- HttpOnly + SameSite cookies for refresh tokens
- Email enumeration protection (same response for existing and non-existing email on reset)
- Input validation via FluentValidation (backend) and Zod (frontend)
- Security headers: CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy
- HTTPS enforced
- File upload validation: allowed extensions + max size

---

## Out of Scope for v1

- Mobile app
- Live video / webinars
- Course comments / forum threads
- Multi-language (i18n)
- Affiliate / referral system
- Instructor payouts
