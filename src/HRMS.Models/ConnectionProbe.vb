Option Strict On
Option Explicit On

Imports System

''' <summary>Result of testing a connection string during setup.</summary>
Public NotInheritable Class ConnectionProbe
    Public Property ServerVersion As String
    ''' <summary>True when no user exists yet (first-run admin must be created).</summary>
    Public Property AdminNeeded As Boolean
End Class
