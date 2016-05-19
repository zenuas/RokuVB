Imports System


Namespace Manager

    Public Class RkLateBind
        Implements IType

        Public Overridable Property Value As IType

        Public Overridable Property Name As String Implements IEntry.Name
            Get
                Return Me.Value.Name
            End Get
            Set(value As String)

                Throw New NotImplementedException()
            End Set
        End Property

        Public Overridable Property [Namespace] As RkNamespace Implements IType.Namespace
            Get
                Return Me.Value.Namespace
            End Get
            Set(value As RkNamespace)

                Throw New NotImplementedException()
            End Set
        End Property

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            Return Me.Value.Is(t)
        End Function
    End Class

End Namespace
