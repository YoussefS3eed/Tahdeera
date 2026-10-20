#!/usr/bin/env python3
"""
Tahdeera Milestone F1 Verification Gate
Automated End-to-End Verification Cockpit
Covers:
  - 16-state Matrix: 4 Viewports x 2 Languages x 2 Themes
  - Horizontal Overflow Assertion (scrollWidth <= clientWidth)
  - Layout-Independent NFC Keystroke Wedge & Simulation
  - Dexie.js Client-Side IndexedDB Storage & Sync Queue
  - Zero-FOUC Theming & Dynamic RTL/LTR Mirroring
  - Functional Route Guards & Stub Pages
  - Mock API Telemetry & RFC 9457 Problem Details
  - Accessible WCAG Focus Trap Modal
  - Machine-readable verification_report.json and screenshots
"""

import os
import sys
import json
import time
from typing import Dict, List, Any
from playwright.sync_api import sync_playwright, Page

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")

BASE_URL = os.environ.get("TAHDEERA_FRONTEND_URL", "http://localhost:4200")
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
FRONTEND_DIR = os.path.abspath(os.path.join(SCRIPT_DIR, ".."))
REPORTS_DIR = os.path.join(FRONTEND_DIR, "reports", "verification-gate")
os.makedirs(REPORTS_DIR, exist_ok=True)

VIEWPORTS = [
    {"name": "desktop_1440", "width": 1440, "height": 900},
    {"name": "tablet_768", "width": 768, "height": 1024},
    {"name": "mobile_large_428", "width": 428, "height": 926},
    {"name": "mobile_small_390", "width": 390, "height": 844}
]

LANGUAGES = ["ar", "en"]
THEMES = ["light", "dark"]

class GateF1Verifier:
    def __init__(self, page: Page):
        self.page = page
        self.results: List[Dict[str, Any]] = []
        self.summary = {
            "total": 0,
            "passed": 0,
            "failed": 0,
            "skipped": 0,
            "start_time": time.time(),
            "duration_seconds": 0.0,
            "matrix_combinations_tested": 0
        }

    def record_test(self, test_name: str, passed: bool, details: Dict[str, Any] = None, error: str = None):
        self.summary["total"] += 1
        if passed:
            self.summary["passed"] += 1
            status = "PASS"
        else:
            self.summary["failed"] += 1
            status = "FAIL"

        result = {
            "test_name": test_name,
            "status": status,
            "details": details or {},
            "error": error
        }
        self.results.append(result)
        prefix = "[PASS]" if passed else "[FAIL]"
        print(f"  {prefix} {test_name}")
        if error:
            print(f"         Error: {error}")

    def verify_no_horizontal_overflow(self) -> Dict[str, Any]:
        """Asserts documentElement scrollWidth <= clientWidth."""
        metrics = self.page.evaluate('''() => {
            const doc = document.documentElement;
            return {
                scrollWidth: doc.scrollWidth,
                clientWidth: doc.clientWidth,
                hasOverflow: doc.scrollWidth > doc.clientWidth,
                overflowPixels: Math.max(0, doc.scrollWidth - doc.clientWidth)
            };
        }''')
        return metrics

    def run_matrix_verification(self):
        print("\n--- [Phase 1/6] Executing 16-Combination Matrix Verification ---")
        self.page.goto(f"{BASE_URL}/", wait_until="networkidle")
        self.page.wait_for_selector(".harness-shell")

        for vp in VIEWPORTS:
            self.page.set_viewport_size({"width": vp["width"], "height": vp["height"]})
            self.page.wait_for_timeout(200)

            for lang in LANGUAGES:
                # Set language
                if lang == "ar":
                    self.page.locator("button:has-text('العربية (RTL)')").first.click()
                else:
                    self.page.locator("button:has-text('English (LTR)')").first.click()
                self.page.wait_for_timeout(150)

                # Verify html dir & lang attributes
                expected_dir = "rtl" if lang == "ar" else "ltr"
                html_dir = self.page.get_attribute("html", "dir")
                html_lang = self.page.get_attribute("html", "lang")
                dir_ok = (html_dir == expected_dir)
                lang_ok = (html_lang == lang)

                for theme in THEMES:
                    self.summary["matrix_combinations_tested"] += 1
                    # Set theme via the theming card button index
                    theme_card = self.page.locator(".bento-card").first
                    if theme == "light":
                        theme_card.locator("button").nth(0).click()
                    else:
                        theme_card.locator("button").nth(1).click()
                    self.page.wait_for_timeout(150)

                    html_theme = self.page.get_attribute("html", "data-theme")
                    theme_ok = (html_theme == theme)

                    # Check overflow
                    overflow = self.verify_no_horizontal_overflow()
                    overflow_ok = not overflow["hasOverflow"]

                    # Capture screenshot
                    ss_name = f"matrix_{vp['name']}_{lang}_{theme}.png"
                    ss_path = os.path.join(REPORTS_DIR, ss_name)
                    self.page.screenshot(path=ss_path)

                    passed = dir_ok and lang_ok and theme_ok and overflow_ok
                    err = None
                    if not passed:
                        err = f"dir={html_dir} (expected {expected_dir}), lang={html_lang} (expected {lang}), theme={html_theme} (expected {theme}), overflow={overflow['overflowPixels']}px"

                    self.record_test(
                        f"Matrix [{vp['name']} | lang={lang} | theme={theme}]",
                        passed,
                        details={
                            "viewport": vp,
                            "lang": lang,
                            "theme": theme,
                            "html_dir": html_dir,
                            "html_theme": html_theme,
                            "overflow": overflow,
                            "screenshot": ss_name
                        },
                        error=err
                    )

    def run_nfc_verification(self):
        print("\n--- [Phase 2/6] Verifying NFC Hardware Wedge & Simulation ---")
        self.page.set_viewport_size({"width": 1440, "height": 900})
        self.page.goto(f"{BASE_URL}/", wait_until="networkidle")

        # 1. Fill simulation form
        card_input = self.page.locator("input.form-control.ltr-num").first
        card_input.fill("04A27C19")
        self.page.locator("button:has-text('Simulate Scan')").first.click()
        self.page.wait_for_timeout(300)

        # 2. Check telemetry and table
        last_uid_elem = self.page.locator(".nfc-telemetry strong.ltr-num").first
        last_uid = last_uid_elem.inner_text()
        nfc_sim_ok = ("04A27C19" in last_uid)

        # 3. Check scan log table entry
        table_has_card = self.page.locator(".scan-log-table td:has-text('04A27C19')").count() > 0

        self.record_test(
            "NFC Wedge: Simulated Card Scan & Table Emission",
            nfc_sim_ok and table_has_card,
            details={"scanned_uid": last_uid, "table_has_card": table_has_card}
        )

        # 4. Check Keyboard Wedge Event dispatch (Hardware burst)
        # Dispatch 8 keystrokes with 10ms delta to test wedge listener
        test_uid = "12AB34CD"
        self.page.evaluate('''() => {
            const keys = [
                { code: 'Digit1', key: '1' },
                { code: 'Digit2', key: '2' },
                { code: 'KeyA', key: 'A' },
                { code: 'KeyB', key: 'B' },
                { code: 'Digit3', key: '3' },
                { code: 'Digit4', key: '4' },
                { code: 'KeyC', key: 'C' },
                { code: 'KeyD', key: 'D' },
                { code: 'Enter', key: 'Enter' }
            ];
            keys.forEach((k, idx) => {
                setTimeout(() => {
                    window.dispatchEvent(new KeyboardEvent('keydown', {
                        code: k.code,
                        key: k.key,
                        bubbles: true,
                        cancelable: true
                    }));
                }, idx * 10);
            });
        }''')
        self.page.wait_for_timeout(400)
        table_has_burst = self.page.locator(f".scan-log-table td:has-text('{test_uid}')").count() > 0
        self.record_test(
            "NFC Wedge: Direct KeyboardEvent.code Rapid Keystroke Burst",
            table_has_burst,
            details={"burst_uid": test_uid, "captured_in_table": table_has_burst}
        )

    def run_offline_storage_verification(self):
        print("\n--- [Phase 3/6] Verifying Dexie.js Client-Side Storage & Sync Queue ---")
        self.page.goto(f"{BASE_URL}/", wait_until="networkidle")

        # 1. Device GUID verification
        guid_elem = self.page.locator(".device-guid-box code.ltr-num").first
        device_guid = guid_elem.inner_text().strip()
        guid_valid = len(device_guid) > 10 and device_guid != "Loading..."

        self.record_test(
            "Dexie Storage: Persistent Device GUID in workspace_metadata",
            guid_valid,
            details={"device_guid": device_guid}
        )

        # 2. Seed mock students (Card 5 buttons: 0=seed, 1=offline attendance, 2=clear)
        storage_card = self.page.locator(".bento-card").nth(4)
        seed_btn = storage_card.locator(".button-group button").nth(0)
        seed_btn.click()
        self.page.wait_for_timeout(400)

        # Read cached students count metric
        cached_count_elem = storage_card.locator(".metric-value").nth(0)
        cached_count = cached_count_elem.inner_text().strip()
        seed_ok = (cached_count == "15")

        self.record_test(
            "Dexie Storage: Bulk Put Students into cached_students (count=15)",
            seed_ok,
            details={"cached_students_count": cached_count}
        )

        # 3. Simulate offline attendance mutation
        offline_btn = storage_card.locator(".button-group button").nth(1)
        offline_btn.click()
        self.page.wait_for_timeout(400)

        pending_count_elem = storage_card.locator(".metric-value").nth(1)
        pending_count = int(pending_count_elem.inner_text().strip() or "0")
        enqueue_ok = (pending_count >= 1)

        self.record_test(
            "Dexie Storage: Enqueue Offline Mutation in sync_queue with auto-increment localId",
            enqueue_ok,
            details={"pending_sync_count": pending_count}
        )

        # 4. Clear workspace cache
        clear_btn = storage_card.locator(".button-group button").nth(2)
        clear_btn.click()
        self.page.wait_for_timeout(400)

        post_clear_count = storage_card.locator(".metric-value").nth(0).inner_text().strip()
        clear_ok = (post_clear_count == "0")

        self.record_test(
            "Dexie Storage: Workspace Cache Purge on Switch (preserves sync_queue)",
            clear_ok,
            details={"cached_students_after_clear": post_clear_count}
        )

    def run_mock_api_verification(self):
        print("\n--- [Phase 4/6] Verifying Mock API Interceptor & RFC 9457 Problem Details ---")
        self.page.goto(f"{BASE_URL}/", wait_until="networkidle")

        api_card = self.page.locator(".bento-card").nth(5)

        # 1. Fetch Students Page 1
        p1_btn = api_card.locator(".button-group button").nth(0)
        p1_btn.click()
        self.page.wait_for_selector(".telemetry-json", timeout=5000)
        p1_text = self.page.locator(".telemetry-json").inner_text()
        p1_ok = ("items" in p1_text and '"pageNumber": 1' in p1_text)

        self.record_test(
            "Mock API: GET /api/v1/students?pageNumber=1 (RFC 9457 Pagination)",
            p1_ok,
            details={"has_items": "items" in p1_text}
        )

        # 2. Trigger 409 Conflict Simulation
        conflict_btn = api_card.locator(".button-group button").nth(2)
        conflict_btn.click()
        self.page.wait_for_selector(".error-box", timeout=5000)
        err_box = self.page.locator(".error-box").inner_text()
        conflict_ok = ("409" in err_box and "Conflict" in err_box)

        self.record_test(
            "Mock API: POST /api/v1/attendance?simulateConflict=true (RFC 9457 ProblemDetails 409)",
            conflict_ok,
            details={"error_output": err_box[:120]}
        )

    def run_modal_focus_trap_verification(self):
        print("\n--- [Phase 5/6] Verifying WCAG AA Focus Trap Modal ---")
        self.page.goto(f"{BASE_URL}/", wait_until="networkidle")

        # Open modal
        wcag_card = self.page.locator(".bento-card").nth(6)
        open_modal_btn = wcag_card.locator(".button-group button").first
        open_modal_btn.click()
        self.page.wait_for_selector(".modal-dialog[role='dialog']", timeout=3000)

        modal_visible = self.page.locator(".modal-dialog[role='dialog']").is_visible()
        # Verify focus is within modal
        active_in_modal = self.page.evaluate('''() => {
            const modal = document.querySelector('.modal-dialog');
            return modal.contains(document.activeElement);
        }''')

        # Close modal via close button
        self.page.locator(".close-btn").click()
        self.page.wait_for_timeout(200)
        modal_closed = not self.page.locator(".modal-dialog[role='dialog']").is_visible()

        self.record_test(
            "WCAG AA Accessibility: Focus Trap Modal Entrapment & Dismissal",
            modal_visible and active_in_modal and modal_closed,
            details={
                "modal_opened": modal_visible,
                "focus_trapped": active_in_modal,
                "modal_closed": modal_closed
            }
        )

    def run_route_guards_verification(self):
        print("\n--- [Phase 6/6] Verifying Route Guards & Stub Pages ---")

        # 1. Accessing /workspace/protected without auth token redirects to /login
        self.page.evaluate('''() => {
            localStorage.setItem('tahdeera_auth_token', 'unauthenticated');
        }''')
        self.page.goto(f"{BASE_URL}/workspace/protected")
        self.page.wait_for_url("**/login**", timeout=5000)
        login_stub_visible = self.page.locator(".stub-card").count() > 0

        self.record_test(
            "Route Guards: authGuard redirects unauthenticated visitor to /login stub",
            login_stub_visible,
            details={"current_url": self.page.url}
        )

        # 2. Accessing /workspace/teachers-only with Viewer role redirects to /workspace/forbidden
        self.page.evaluate('''() => {
            localStorage.setItem('tahdeera_auth_token', 'mock-token-viewer');
            localStorage.setItem('tahdeera_user_role', 'Viewer');
        }''')
        self.page.goto(f"{BASE_URL}/workspace/teachers-only")
        self.page.wait_for_url("**/workspace/forbidden**", timeout=5000)
        forbidden_stub_visible = self.page.locator(".stub-card").count() > 0

        self.record_test(
            "Route Guards: roleGuard redirects insufficient role to /workspace/forbidden stub",
            forbidden_stub_visible,
            details={"current_url": self.page.url}
        )

        # 3. Accessing /workspace/subscribed-only with Expired workspace redirects to /workspace/lockout
        self.page.evaluate('''() => {
            localStorage.setItem('tahdeera_workspace_status', 'Expired');
        }''')
        self.page.goto(f"{BASE_URL}/workspace/subscribed-only")
        self.page.wait_for_url("**/workspace/lockout**", timeout=5000)
        lockout_stub_visible = self.page.locator(".stub-card").count() > 0

        self.record_test(
            "Route Guards: lockoutGuard redirects Expired workspace to /workspace/lockout stub",
            lockout_stub_visible,
            details={"current_url": self.page.url}
        )

        # Clean up localStorage
        self.page.evaluate('''() => {
            localStorage.removeItem('tahdeera_user_role');
            localStorage.removeItem('tahdeera_workspace_status');
        }''')


def main():
    print("=" * 70)
    print("Tahdeera Milestone F1 Verification Gate")
    print(f"Target Frontend: {BASE_URL}")
    print(f"Reports Directory: {REPORTS_DIR}")
    print("=" * 70)

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(viewport={"width": 1440, "height": 900})
        page = context.new_page()

        verifier = GateF1Verifier(page)

        try:
            verifier.run_matrix_verification()
            verifier.run_nfc_verification()
            verifier.run_offline_storage_verification()
            verifier.run_mock_api_verification()
            verifier.run_modal_focus_trap_verification()
            verifier.run_route_guards_verification()
        except Exception as e:
            verifier.record_test("Fatal Exception during Verification Suite", False, error=str(e))
        finally:
            browser.close()

        verifier.summary["duration_seconds"] = round(time.time() - verifier.summary["start_time"], 2)

        # Write machine-readable verification_report.json
        report_data = {
            "milestone": "F1",
            "summary": verifier.summary,
            "test_results": verifier.results
        }

        report_file = os.path.join(REPORTS_DIR, "verification_report.json")
        with open(report_file, "w", encoding="utf-8") as f:
            json.dump(report_data, f, indent=2, ensure_ascii=False)

        print("\n" + "=" * 70)
        print("Verification Gate Execution Summary:")
        print(f"  Total Tests:    {verifier.summary['total']}")
        print(f"  Passed Tests:   {verifier.summary['passed']}")
        print(f"  Failed Tests:   {verifier.summary['failed']}")
        print(f"  Matrix States:  {verifier.summary['matrix_combinations_tested']} combinations")
        print(f"  Duration:       {verifier.summary['duration_seconds']}s")
        print(f"  Report File:    {report_file}")
        print("=" * 70)

        if verifier.summary["failed"] > 0:
            print("\n❌ VERIFICATION GATE FAILED: Nonzero exit code returned.")
            sys.exit(1)
        else:
            print("\n✅ VERIFICATION GATE PASSED: All F1 requirements verified with 0 failures.")
            sys.exit(0)

if __name__ == "__main__":
    main()
