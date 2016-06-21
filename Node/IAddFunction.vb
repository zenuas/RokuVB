Imports System.Collections.Generic


Namespace Node

    Public Interface IAddFunction

        ReadOnly Property Functions As List(Of FunctionNode)

        Sub AddFunction(func As FunctionNode)

    End Interface

End Namespace
