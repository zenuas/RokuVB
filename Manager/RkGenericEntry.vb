Imports System
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkGenericEntry
        Implements IType

        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overridable Property Name As String Implements IType.Name
        Public Overridable Property ApplyIndex As Integer
        Public Overridable Property Reference As IApply = Nothing

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            If Me.Reference Is Nothing Then Return True
            Dim self = Me.ToType
            If self Is Nothing Then Return True
            Return self.Is(t)
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

        Public Overridable Function ToType() As IType

            Return Me.Reference.Apply(Me.ApplyIndex)
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Dim t = Me.ToType
            Return t Is Nothing OrElse t.HasGeneric
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Dim t = Me.ToType
            Return t IsNot Nothing AndAlso t.HasIndefinite
        End Function

        Public Overrides Function ToString() As String

            Dim t = Me.ToType
            Return If(t Is Nothing, Me.Name, t.ToString)
        End Function
    End Class

End Namespace
