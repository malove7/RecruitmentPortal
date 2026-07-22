---
name: Recruitment Portal
description: Internal HR/recruitment pipeline tool — indigo-on-charcoal command surface for tracking candidates, interviews, and offers.
colors:
  indigo-primary: "#6366f1"
  indigo-primary-hover: "#4f46e5"
  gradient-violet-start: "#7c3aed"
  gradient-indigo-end: "#6366f1"
  slate-secondary: "#64748b"
  slate-secondary-hover: "#475569"
  emerald-success: "#10b981"
  amber-warning: "#f59e0b"
  red-danger: "#ef4444"
  sky-info: "#3b82f6"
  paper-bg: "#f8fafc"
  card-bg: "#ffffff"
  charcoal-sidebar: "#111827"
  ink-primary: "#111827"
  ink-secondary: "#6b7280"
  sidebar-ink: "#9ca3af"
  sidebar-ink-hover: "#ffffff"
  border-neutral: "#e5e7eb"
typography:
  headline:
    fontFamily: "Inter, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    fontSize: "1.75rem"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "-0.025em"
  title:
    fontFamily: "Inter, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    fontSize: "1.25rem"
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: "normal"
  body:
    fontFamily: "Inter, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: "normal"
  label:
    fontFamily: "Inter, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 600
    lineHeight: 1.4
    letterSpacing: "0.05em"
rounded:
  sm: "6px"
  md: "8px"
  lg: "12px"
  xl: "16px"
  pill: "9999px"
spacing:
  1: "4px"
  2: "8px"
  3: "12px"
  4: "16px"
  6: "24px"
  8: "32px"
components:
  button-primary:
    backgroundColor: "linear-gradient(135deg, {colors.gradient-violet-start}, {colors.gradient-indigo-end})"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    padding: "8px 16px"
  button-primary-hover:
    backgroundColor: "linear-gradient(135deg, {colors.indigo-primary-hover}, {colors.gradient-indigo-end})"
  button-secondary:
    backgroundColor: "#ffffff"
    textColor: "{colors.ink-primary}"
    rounded: "{rounded.md}"
    padding: "8px 16px"
  card:
    backgroundColor: "{colors.card-bg}"
    rounded: "{rounded.lg}"
    padding: "24px"
  badge-success:
    backgroundColor: "rgba(16, 185, 129, 0.1)"
    textColor: "{colors.emerald-success}"
    rounded: "{rounded.pill}"
    padding: "4px 12px"
  badge-warning:
    backgroundColor: "rgba(245, 158, 11, 0.1)"
    textColor: "{colors.amber-warning}"
    rounded: "{rounded.pill}"
    padding: "4px 12px"
  badge-danger:
    backgroundColor: "rgba(239, 68, 68, 0.1)"
    textColor: "{colors.red-danger}"
    rounded: "{rounded.pill}"
    padding: "4px 12px"
---

# Design System: Recruitment Portal

## 1. Overview

**Creative North Star: "The Ops Desk"**

No metaphor beyond what it is: well-organized internal tooling for running a recruitment pipeline. A dark charcoal sidebar anchors the shell; the working surface stays a plain, near-white paper so tables and forms stay the loudest thing on screen. One indigo-to-violet gradient carries all primary action and selection state — it never spreads past buttons, active nav, and stat-icon accents. Status is color-coded but never color-only: every badge pairs a hue with a text label. Cards lift 2-4px on hover as the only real motion in the system; everything else is instant.

This system explicitly rejects bloated enterprise SaaS (dense nested menus, modal-on-modal workflows, Salesforce/SAP-style overload) and the generic AI-template look (gradient text, glassmorphism, cream/tan backgrounds, identical icon-card grids, uppercase-eyebrow section scaffolding). The existing indigo-on-charcoal identity is the anchor; new screens extend it, they don't reinvent it.

**Key Characteristics:**
- Flat near-white working surface, charcoal sidebar shell
- One accent gradient (violet → indigo), used sparingly: buttons, active nav, stat icons
- Status badges: tinted background + saturated text, never color alone
- Subtle shadow-and-lift on hover; flat at rest
- 14px body base, dense but breathing (24-32px section gaps)

## 2. Colors

Two-tone shell (charcoal sidebar, paper canvas) plus one accent gradient and a standard semantic status set.

### Primary
- **Indigo Primary** (#6366f1): Links, focus rings, primary icon accents, base of the primary-button gradient.
- **Indigo Primary Hover** (#4f46e5): Hover state for primary actions and links.
- **Gradient Violet → Indigo** (#7c3aed → #6366f1, 135deg): The only gradient in the system. Reserved for `.btn-primary` and nothing else — not headings, not text, not backgrounds.

### Secondary
- **Slate Secondary** (#64748b): Secondary text emphasis, secondary-button borders on hover.

### Neutral
- **Paper** (#f8fafc): Page background (`--bg-body`) and table-header background — the quiet working surface.
- **Card White** (#ffffff): Card, table, and secondary-button background.
- **Charcoal Sidebar** (#111827): Sidebar shell background — the one dark surface in the system.
- **Ink Primary** (#111827): Headings and primary body text on light surfaces.
- **Ink Secondary** (#6b7280): Muted text, labels, table-header text.
- **Sidebar Ink** (#9ca3af) / **Sidebar Ink Hover** (#ffffff): Nav-link text at rest and on hover/active, against the charcoal shell.
- **Border Neutral** (#e5e7eb): All card, table, and input borders/dividers.

### Status (semantic, not decorative)
- **Emerald Success** (#10b981), **Amber Warning** (#f59e0b), **Red Danger** (#ef4444), **Sky Info** (#3b82f6): each always paired with a `rgba(color, 0.1)` tint background and a text label inside a badge — never a bare color dot or color-only signal.

### Named Rules
**The One Gradient Rule.** The violet→indigo gradient exists in exactly one place at a time: the primary button and the active-nav accent glow. It never appears on text, headings, or section backgrounds.

**The Label-Plus-Color Rule.** Every status indicator (candidate stage, interview outcome, evaluation status) pairs its color with a text label. Color alone never carries meaning — required both for accessibility and because HR/Interviewer users scan fast.

## 3. Typography

**Display/Headline Font:** Inter, with 'Segoe UI', Roboto, Helvetica, Arial, sans-serif fallback
**Body Font:** Inter (same stack)
**Label Font:** Inter, uppercase, tracked

**Character:** A single, efficient system sans across every role. No secondary display face — the personality comes from weight and size steps, not font pairing, which suits a fast internal tool over a branded editorial surface.

### Hierarchy
- **Headline** (700, 1.75rem/28px, 1.2 line-height, -0.025em tracking): Page titles (`.page-title`), `h1`.
- **Title** (600, 1.25rem/20px): Section headers (`.section-title`), `h3`, card headers.
- **Body** (400, 0.875rem/14px, 1.5 line-height): Default UI text, table cells, form inputs — the base size for the whole app.
- **Label** (600, 0.75rem/12px, 0.05em tracking, uppercase): Table column headers, badges.

### Named Rules
**The 14px Base Rule.** Body text is 14px, not 16px — this is dense operational tooling read at a desk, not long-form content optimized for reading comfort. Don't inflate base size chasing generic "accessibility" defaults; contrast and hierarchy carry legibility instead.

## 4. Elevation

Flat at rest, lifted on interaction. Cards and stat tiles carry a barely-there `shadow-sm` at rest and step up to `shadow-md` plus a 2-4px upward translate on hover — the only place elevation communicates anything. Nothing else in the system uses shadow; the sidebar and page background are pure flat color.

### Shadow Vocabulary
- **shadow-sm** (`0 1px 2px 0 rgb(0 0 0 / 0.05)`): Resting state for cards, table containers, buttons.
- **shadow-md** (`0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)`): Hover state for `.card` and `.stat-card`.
- **shadow-lg** (`0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)`): Mobile sidebar overlay, floating sidebar-toggler button.

### Named Rules
**The Hover-Only Rule.** Shadow growth and lift are a response to hover, never a resting decoration. If an element isn't interactive, it stays flat.

## 5. Components

### Buttons
- **Shape:** 8px radius (`--radius-md`).
- **Primary:** Violet→indigo gradient background, white text, `0.5rem 1rem` padding, `shadow-sm` at rest.
- **Hover / Focus:** Gradient shifts toward `--primary-hover`, `scale(1.02)`, and a `0 0 0 3px rgba(99,102,241,0.2)` focus glow — no color-only hover, always a shape/shadow change too.
- **Secondary:** White background, 1px `--border-color` border, ink-primary text; hover fills to `--bg-body` and darkens the border.
- **Danger:** Solid `--danger-color`, white text — reserved for destructive actions only (delete candidate, revoke role).

### Cards / Containers
- **Corner Style:** 12px radius (`--radius-lg`).
- **Background:** Card White on Paper page background — the contrast that defines the surface.
- **Shadow Strategy:** See Elevation — `shadow-sm` at rest, `shadow-md` + `translateY(-2px)` on hover.
- **Border:** 1px `--border-color`, tightens to a faint indigo tint (`rgba(99,102,241,0.3)`) on stat-card hover.
- **Internal Padding:** 24px (`--space-6`) for card bodies, 24px header padding.

### Stat Tiles (signature component)
48px square icon tile (8px radius) in a semantic tint (`rgba(color, 0.1)` bg, solid color icon), a 2rem/700-weight value, and a 14px/500-weight muted label below. Hover lifts 4px, deeper than the standard card's 2px — stat tiles are the dashboard's primary scan target, so their hover response is slightly more pronounced.
- **Known gap:** `Views/Home/Index.cshtml` uses `.stat-icon.info` for "Interviews Today", but `dashboard.css` only defines `.stat-icon.primary/.success/.warning/.danger` — the info variant falls back to no background/color. Add `.stat-icon.info { background-color: rgba(59, 130, 246, 0.1); color: var(--info-color); }` alongside the other three.

### Inputs / Fields
- **Style:** 8px radius, 1px `--border-color`, `0.625rem 0.75rem` padding.
- **Focus:** Border shifts to indigo primary plus a `0 0 0 3px rgba(99,102,241,0.1)` glow — same focus language as buttons, for a consistent "this is now active" signal across the system.

### Navigation (sidebar)
- **Style:** Fixed 260px charcoal (`--bg-sidebar`) column, sidebar-ink text at rest, white on hover/active.
- **Active state:** Soft indigo gradient wash (`rgba(99,102,241,0.2)` → `rgba(99,102,241,0.05)`, 90deg) plus a 3px left accent border. This is a **selected-state indicator on a single-column nav**, not a decorative stripe on a card — the one legitimate use of a border-accent in this system.
- **Responsive:** Collapses to an 80px icon rail ≤992px (labels hidden, active indicator moves to a bottom border); becomes an off-canvas drawer with a floating circular toggle ≤768px.

### Tables
- **Header:** Paper background, ink-secondary text, uppercase 12px label styling, bottom border.
- **Rows:** 16px/24px cell padding, bottom border between rows, no border on the last row, hover tints to Paper.
- **Container:** theme.css defines a dedicated `.table-container` wrapper (12px radius, `shadow-sm`, 1px border), but real views (`Candidates/Index`, `Home/Index`) instead wrap `.table` directly in `.card`/`.card-body`, using Bootstrap's `bg-light` thead and `border-top-0` cells rather than the `.table th`/`.table td` rules. Both read fine visually since `.card` already supplies the same radius/shadow/border, but it means `.table-container` is currently dead CSS. Pick one: either standardize new tables on `.table-container` (and drop the `.card` wrapper), or delete `.table-container` and treat `.card > .table` as the canonical pattern. Don't introduce a third variant.

### Badges
- Pill-shaped (9999px radius), `0.25rem 0.75rem` padding, 12px/500-weight text, tinted background per status color. Used for candidate stage, interview round, evaluation status — always with a text label per the Label-Plus-Color Rule.
- **Known gap:** `Views/Candidates/Index.cshtml` and `Views/Home/Index.cshtml` both emit `badge-info` for `CandidateStatus.InReview`, but `theme.css` only defines `badge-primary/blue`, `badge-success/green`, `badge-warning/yellow`, `badge-danger/red`, `badge-secondary/gray`. `badge-info` renders unstyled. Add a `badge-info` rule using `--info-color` (#3b82f6, same as Sky Info) rather than reusing `badge-blue` — In-Review and Applied are different stages and shouldn't share a color.

### Avatar
Initials circle: 36px diameter, `rounded-circle`, indigo-tinted background (`rgba(99, 102, 241, 0.1)`), indigo text, 600-weight initials centered. Used as the leading element in candidate-name table cells (`Views/Candidates/Index.cshtml`). Currently authored inline (`style="width:36px;height:36px;background-color:rgba(99,102,241,0.1)"`) rather than as a reusable class — promote to a `.avatar` utility in theme.css so size/tint stay consistent if reused elsewhere (interviewer lists, user tables).

### Empty States
Centered muted single-line message inside the table body (`<td colspan class="text-center py-4 text-muted">No recent candidates found.</td>`), used on the dashboard's Recent Candidates and Upcoming Interviews panels. Minimal but consistent; extend this pattern (not a new one) for other empty lists rather than leaving a blank table.

## 6. Do's and Don'ts

### Do:
- **Do** keep the primary gradient confined to `.btn-primary` and the active-nav wash — one accent, used sparingly, per the One Gradient Rule.
- **Do** pair every status color with a text label (badges, not color dots) per the Label-Plus-Color Rule.
- **Do** reuse the existing card / table / badge / button system for new screens rather than inventing new patterns per feature.
- **Do** keep shadows as a hover-only response (Hover-Only Rule); flat at rest everywhere else.
- **Do** hide role-gated nav items and actions outright for users without permission, rather than showing them disabled with an explanation.

### Don't:
- **Don't** ship bloated enterprise SaaS patterns: no dense nested menus, no modal-on-modal workflows, no Salesforce/SAP-style information overload.
- **Don't** reach for the generic AI-template look: no gradient text, no glassmorphism, no cream/tan/sand backgrounds, no identical icon-card grids, no uppercase-tracked eyebrows scaffolding every section.
- **Don't** add a second gradient, a second dark surface, or a second display font — the palette is deliberately narrow.
- **Don't** use `border-left`/`border-right` as a decorative accent on cards, alerts, or list items. The sidebar's active-nav border is the one sanctioned exception (a real selected-state indicator on a single-column nav), not a precedent for callouts or cards elsewhere.
- **Don't** signal status with color alone — always add the text label.
- **Don't** inflate the 14px body base chasing generic "readability" — this is dense operational tooling, not long-form content.
- **Don't** reference a badge/icon color variant (`badge-info`, `stat-icon.info`, etc.) that isn't defined in theme.css/dashboard.css. Two real instances exist today (`badge-info`, `.stat-icon.info`) and both render unstyled — define the variant first, or reuse an existing one.
