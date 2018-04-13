Imports System.Collections.Generic


Namespace Manager

    Public Interface IScope
        Inherits IClosure

        ReadOnly Property Name As String
        Property Parent As IScope
        ReadOnly Property Structs As Dictionary(Of String, List(Of IStruct))
        ReadOnly Property Functions As Dictionary(Of String, List(Of IFunction))

        Sub AddStruct(x As IStruct)
        Sub AddStruct(x As IStruct, name As String)

        Sub AddFunction(x As IFunction)
        Sub AddFunction(x As IFunction, name As String)

        Function FindCurrentStruct(name As String) As IEnumerable(Of IStruct)
        Function FindCurrentFunction(name As String) As IEnumerable(Of IFunction)

    End Interface

End Namespace
