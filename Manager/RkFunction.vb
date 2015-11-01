Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Manager

    Public Class RkFunction
        Inherits RkStruct

        Public Overridable ReadOnly Property Arguments As List(Of NamedValue(Of IType))
        Public Overridable Property [Return] As IType
        Public Overridable ReadOnly Property Body As List(Of RkCode0)

    End Class

End Namespace
