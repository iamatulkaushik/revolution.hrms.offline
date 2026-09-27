Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Global reference lists: states, districts, banks.</summary>
Public NotInheritable Class RefRepo
    Private Sub New()
    End Sub

    Public Shared Function States() As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_State_List")
        End Using
    End Function

    Public Shared Function Districts(stateID As Integer) As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_District_List", Db.P("@StateID", stateID))
        End Using
    End Function

    Public Shared Function Banks() As DataTable
        Using cn As SqlConnection = Db.OpenSession()
            Return Db.Table(cn, "mst.usp_Bank_List")
        End Using
    End Function
End Class