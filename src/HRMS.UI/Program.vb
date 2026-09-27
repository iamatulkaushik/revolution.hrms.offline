Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Threading
Imports System.Windows.Forms
Imports HRMS.BLL
Imports HRMS.Common

''' <summary>Start-up flow: setup (if needed) -> sign in -> company -> forced password change -> main window.
''' After Log out the loop starts again at sign in.</summary>
Public Module Program
    Public Function Main() As Integer
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        AddHandler Application.ThreadException, AddressOf OnThreadException
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnDomainException

        Try
            If Not Startup() Then Return 1
            Do
                If Not RunSession() Then Exit Do
            Loop
            Return 0
        Catch ex As Exception
            Logger.Failure("Fatal error", ex)
            ErrorHandler.Show(Nothing, ex)
            Return 1
        End Try
    End Function

    ''' <summary>Loads configuration and the key; shows setup when something is missing.</summary>
    Private Function Startup() As Boolean
        Dim protector As IKeyProtector = New DpapiKeyProtector()
        Dim keys As New KeyStore(protector, KeyStore.DefaultPath())
        Dim auth As New AuthService()
        Dim cs As String = Nothing
        Dim problem As String = Nothing
        Dim needSetup As Boolean = False

        Try
            cs = ConnectionStringStore.Load(protector)
            SetupService.Configure(cs)
            needSetup = auth.IsBootstrapNeeded()   ' also proves the database is reachable
        Catch ex As BusinessException
            problem = ex.Message
            needSetup = True
        End Try
        If Not keys.Exists() Then needSetup = True

        If needSetup Then
            Using f As New frmSetup(protector, keys, problem)
                If f.ShowDialog() <> DialogResult.OK Then Return False
            End Using
            cs = ConnectionStringStore.Load(protector)
        End If

        AppServices.Initialize(auth, New FieldCrypto(keys.Load()), SetupService.ServerName(cs))
        FormRegistry.RegisterDefaults()
        Return True
    End Function

    ''' <summary>One sign-in to log-out cycle. Returns True to show sign-in again, False to quit.</summary>
    Private Function RunSession() As Boolean
        Dim auth As AuthService = AppServices.Auth
        Dim outcome As LoginOutcome
        Using f As New frmLogin(AppServices.ServerName)
            If f.ShowDialog() <> DialogResult.OK Then Return False
            outcome = f.Outcome
        End Using

        Try
            If Not ChooseCompany(auth, outcome) Then
                auth.Logout()
                Return True
            End If
            If AppSession.Require().MustChangePassword Then
                Using f As New frmChangePassword(True)
                    If f.ShowDialog() <> DialogResult.OK Then
                        auth.Logout()
                        Return True
                    End If
                End Using
            End If
            Using main As New frmMain()
                Application.Run(main)
                Dim again As Boolean = main.LogoutRequested
                auth.Logout()
                Return again
            End Using
        Catch ex As Exception
            ErrorHandler.Show(Nothing, ex)
            auth.Logout()
            Return True
        End Try
    End Function

    Private Function ChooseCompany(auth As AuthService, outcome As LoginOutcome) As Boolean
        Dim companies As DataTable = auth.GetCompanies()
        Dim isAssociate As Boolean = outcome.UserType = "Associate"

        If companies.Rows.Count = 0 Then
            If Not isAssociate Then
                ErrorHandler.ShowInfo(Nothing, "No active company is assigned to your account. Ask your administrator.")
                Return False
            End If
            auth.CompleteLogin(Nothing)
            ErrorHandler.ShowInfo(Nothing, "No company exists yet. Create one from Masters > Company.")
            Return True
        End If

        If companies.Rows.Count = 1 Then
            auth.CompleteLogin(Convert.ToInt32(companies.Rows(0)("CompanyID")))
            Return True
        End If

        Using f As New frmCompanySelect(companies, isAssociate, "Log out")
            If f.ShowDialog() <> DialogResult.OK Then Return False
            auth.CompleteLogin(f.SelectedCompanyID)
            Return True
        End Using
    End Function

    Private Sub OnThreadException(sender As Object, e As ThreadExceptionEventArgs)
        ErrorHandler.Show(Form.ActiveForm, e.Exception)
    End Sub

    Private Sub OnDomainException(sender As Object, e As UnhandledExceptionEventArgs)
        Logger.Failure("Unhandled exception (terminating=" & e.IsTerminating & ")", TryCast(e.ExceptionObject, Exception))
    End Sub
End Module
