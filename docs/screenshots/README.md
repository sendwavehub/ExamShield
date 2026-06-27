# Screenshots & Demo GIFs

Place screenshot and GIF assets here. Reference them from the root `README.md`.

## Naming convention

```
capture-flow.gif          — Flutter capture flow end-to-end
dashboard-overview.png    — Admin dashboard main view
security-center.png       — Security Center / SOC view
audit-timeline.png        — Audit log timeline
manual-review.png         — Two-panel manual review screen
device-management.png     — Device grid with trust status
public-verify.png         — Anonymous QR / hash verification page
```

## How to record a demo GIF

**macOS:**
```bash
# Install LICEcap (https://www.cockos.com/licecap/) or use QuickTime + gifski
gifski --fps 15 --quality 85 -o capture-flow.gif capture-flow.mov
```

**Linux:**
```bash
# Install peek or silentcast
peek   # GUI GIF recorder — click record, perform action, stop
```

**Cross-platform (browser):**
Use [ScreenToGif](https://www.screentogif.com/) (Windows) or Chrome DevTools → Record.

## Recommended dimensions

| Asset | Size |
|---|---|
| Full-page screenshot | 1440 × 900 |
| Mobile screenshot | 390 × 844 (iPhone 14 Pro) |
| GIF — short flow | ≤ 1200px wide, ≤ 30 s, ≤ 5 MB |
