Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Interface IType
        Inherits IEntry

        Function DefineGeneric(name As String) As RkGenericEntry
        Function FixedGeneric(ParamArray values() As IType) As IType
        Function FixedGeneric(ParamArray values() As NamedValue(Of IType)) As IType
        Function HasGeneric() As Boolean

    End Interface

End Namespace
