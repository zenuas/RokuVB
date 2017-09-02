Imports System.Collections.Generic


Namespace Manager

    Public Interface IScope

        ReadOnly Property Name As String
        Property Parent As IScope
        ReadOnly Property Structs As Dictionary(Of String, List(Of RkStruct))
        ReadOnly Property Functions As Dictionary(Of String, List(Of IFunction))

        Sub AddStruct(x As RkStruct)
        Sub AddStruct(x As RkStruct, name As String)

        Sub AddFunction(x As IFunction)
        Sub AddFunction(x As IFunction, name As String)

        Function FindCurrentStruct(name As String) As IEnumerable(Of RkStruct)
        Function FindCurrentFunction(name As String) As IEnumerable(Of IFunction)

    End Interface

End Namespace
