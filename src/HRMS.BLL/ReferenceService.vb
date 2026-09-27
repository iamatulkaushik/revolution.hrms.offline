Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports HRMS.Common
Imports HRMS.DAL

''' <summary>States, districts and banks for dropdowns.</summary>
Public NotInheritable Class ReferenceService
    Public Function States() As DataTable
        AppSession.Require()
        Return RefRepo.States()
    End Function

    Public Function Districts(stateID As Integer) As DataTable
        AppSession.Require()
        Return RefRepo.Districts(stateID)
    End Function

    Public Function Banks() As DataTable
        AppSession.Require()
        Return RefRepo.Banks()
    End Function
End Class
