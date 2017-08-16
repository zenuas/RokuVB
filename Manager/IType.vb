Namespace Manager

    Public Interface IType
        Inherits IEntry

        Property [Namespace] As RkNamespace

        Function GetValue(name As String) As IType
        Function [Is](t As IType) As Boolean

        Function DefineGeneric(name As String) As RkGenericEntry
        Function FixedGeneric(ParamArray values() As IType) As IType
        Function FixedGeneric(ParamArray values() As NamedValue) As IType
        Function HasGeneric() As Boolean
        Function CloneGeneric() As IType
        Function Indefinite() As Boolean

    End Interface

End Namespace
