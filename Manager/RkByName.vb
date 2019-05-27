Imports System
Imports Roku.Manager.SystemLibrary
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkByName
        Implements IType

        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable Property Type As IType

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            Return FixedByName(Me).Is(t)
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Return values(0)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Return values.FindFirst(Function(x) x.Name.Equals(Me.Name)).Value
        End Function

        Public Overridable Function TypeToApply(value As IType) As IType() Implements IType.TypeToApply

            Return {value}
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Type Is Nothing OrElse Me.Type.HasGeneric
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return Me.Type IsNot Nothing AndAlso Me.Type.HasIndefinite
        End Function

        Public Overrides Function ToString() As String

            Return Me.Type?.ToString
        End Function
    End Class

End Namespace
