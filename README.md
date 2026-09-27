# HRMS — .NET Solution (Phase 2)

VB.NET, .NET Framework 4.8, SQL Server. Open `HRMS.sln` in Visual Studio 2019/2022.

## Projects
| Project | Role |
|---|---|
| `HRMS.Common` | crypto, key store, logging, formats, validators, session |
| `HRMS.Models` | plain data classes |
| `HRMS.DAL` | `Db` helper + repositories (stored procs only) |
| `HRMS.BLL` | services: auth, company, masters, employee |
| `HRMS.UI` | WinForms app `RevolutionHRMS.exe`: setup, sign-in, company, lock, main menu |
| `HRMS.SmokeTest` | console tests: offline self-tests + end-to-end DB test |

Layer rule: UI -> BLL -> DAL -> procs. `HRMS.UI` references Common, Models and BLL only (never DAL).

## Repo layout (suggested for GitHub)
```
docs/   prd.md, Architecture.md, rules.md, design.md, task.md
db/     scripts/0001..0012, tools/, run_all.bat
src/    this folder (HRMS.sln + src/)
```

## Run the tests
1. Apply DB scripts `0001`-`0017` (`db\run_all.bat`, now up to `0017`).
2. Set the app connection string (hrms_app login from `0007`):
```
set HRMS_CONN=Server=.\SQLEXPRESS;Database=HRMS_Data;User Id=hrms_app;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True
```
3. Offline tests only: `HRMS.SmokeTest.exe`
4. With database: `HRMS.SmokeTest.exe --db`
   - Fresh DB: it creates user `admin` with the password you enter (8+ chars, letters + digits)
   - Existing DB: enter the admin password you set earlier
   - Or set `HRMS_ADMIN_PWD` to skip the prompt
5. Remove test data: `db\tools\cleanup_smoke.sql`

## Encryption key (important)
- First run creates `C:\ProgramData\RevolutionHRMS\Keys\field.key` (DPAPI-wrapped, this PC only)
- Encrypts Aadhaar, PAN, bank a/c, UAN, ESI IP
- LAN: every PC needs the SAME key: `KeyStore.ExportToFile(path, passphrase)` here, `ImportFromFile` on the others
- Back up the export file offline. Lose the key = lose those fields

## Notes
- Sessions: each DAL call opens a connection and runs `sec.usp_Session_Set` (keys are read-only per connection)
- If the second call in a run fails with "read-only" session context, add `Pooling=False` to the connection string and report it
- `TrustServerCertificate=True` is for a self-signed server cert. Install a proper cert later and remove it
- Iterations: 310,000 PBKDF2-SHA256 per login (about 0.1-0.3 s)
- Verified so far: all 5 projects compile against .NET 4.8 reference assemblies; the 46 offline tests pass on a .NET runtime. Database tests need your SQL Server

## Run the app (Phase 3)
Set `HRMS.UI` as the startup project and press F5. Flow:
1. **Setup** (only when needed): database connection -> encryption key -> first administrator
2. **Sign in**: 5 wrong passwords lock the account for 15 minutes
3. **Company**: Associates pick one (or "No company" to create the first); other users go straight in
4. **Forced password change** for new accounts
5. **Main window**: menu shows only what the user's role allows. Ctrl+L locks; 10 minutes idle locks automatically
- Dev shortcut: set env var `HRMS_CONN` to skip the saved connection string
- Menu items for later phases show "planned for Phase N" until their form is registered in `FormRegistry`
- Forms are built in code (no designer files) for a consistent theme; child screens can use the designer later
- Setup asks for the database password again if it is re-run (only the connection string is stored, DPAPI-protected)
- Not yet visually tested: I could only compile the UI, not run it. Report layout glitches with a screenshot

## Screens so far (Phase 4)
Masters menu: Company (4 tabs), Division, Factory / site, Department, Designation, Employee (list + 6-tab editor with nominees).
Still to come in Phase 4: Minimum wages, Holiday calendar, Financial year.
Employee editor tabs: Personal, Job, IDs and bank (encrypted), Statutory, More details, Nominees.

## Monthly attendance (Phase 5)
Attendance menu > Monthly attendance. Pick month, year and site, then Load.
- Columns: Worked, Holidays, CL, EL, SL, Comp, OT hours. Paid days = the first six added; Absent = days in month - paid days
- Days are whole or half days. A row turns red when more days are entered than the employee could have worked (joining and leaving dates)
- Fill empty rows: sets Worked for rows still at zero. Copy last month: fills empty rows from the previous month
- Import file: .xlsx or .csv with a header row. Recognised headers: Code, WD, HD, CL, EL, SL, Comp, OT, Remarks (any case). Export CSV gives you the template
- Nothing is written until Save (Ctrl+S). All rows save in one transaction
- Close month locks it. Reopen needs a reason of 10+ characters and is blocked once that month's payroll is approved. Only users with ATT_CLOSE (Company Admin, Associate Admin) can close or reopen
- Ctrl+S save, F5 reload
