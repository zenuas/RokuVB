Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkGenericEntry
        Implements IType

        Public Overridable Property Name As String Implements IType.Name
        Public Overridable Property Reference As IType = Nothing

    End Class

End Namespace
