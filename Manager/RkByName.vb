Imports System


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

            Return False
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return False
        End Function

        Public Overrides Function ToString() As String

            Return Me.Name
        End Function
    End Class

End Namespace
