Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Interface INamedFunction
        Inherits INode

        Property Name As String
        Property [Function] As RkFunction
        Property Bind As Dictionary(Of INamedFunction, Boolean)

    End Interface

End Namespace
