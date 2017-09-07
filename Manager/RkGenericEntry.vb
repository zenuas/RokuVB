Imports System
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkGenericEntry
        Implements IType

        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overridable Property Name As String Implements IType.Name
        Public Overridable Property ApplyIndex As Integer
        'Public Overridable Property Reference As IType = Nothing

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            Return True
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Return values.FindFirst(Function(x) x.Name.Equals(Me.Name)).Value
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return True
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return False
        End Function

        Public Overrides Function ToString() As String

            Return Me.Name
        End Function
    End Class

End Namespace
