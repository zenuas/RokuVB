Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Manager

    Public Class RkByName
        Implements IType

        Public Overridable Property [Namespace] As RkNamespace Implements IType.Namespace
        Public Overridable Property Name As String Implements IEntry.Name

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Function [Is](t As IType) As Boolean Implements IType.Is

            Throw New NotImplementedException()
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Throw New NotImplementedException()
        End Function
    End Class

End Namespace
