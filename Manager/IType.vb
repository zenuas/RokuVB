Namespace Manager

    Public Interface IType
        Inherits IEntry

        Property Scope As IScope

        Function GetValue(name As String) As IType
        Function [Is](t As IType) As Boolean

        Function DefineGeneric(name As String) As RkGenericEntry
        Function FixedGeneric(ParamArray values() As IType) As IType
        Function FixedGeneric(ParamArray values() As NamedValue) As IType
        Function TypeToApply(value As IType) As IType()
        Function HasGeneric() As Boolean
        Function CloneGeneric() As IType
        Function HasIndefinite() As Boolean

    End Interface

End Namespace
