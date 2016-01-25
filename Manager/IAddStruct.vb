Namespace Manager

    Public Interface IAddStruct

        Sub AddStruct(x As RkStruct)
        Sub AddStruct(x As RkStruct, name As String)
        Function GetStruct(name As String, ParamArray args() As IType) As RkStruct

    End Interface

End Namespace
