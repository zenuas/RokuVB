Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Interface IScopeNode
        Inherits INode

        ReadOnly Property Scope As Dictionary(Of String, INode)
        'ReadOnly Property [Imports] As List(Of ImportEntry)

        Sub AddFunction(func As FunctionNode)
        Sub AddVar(var_ As VariableNode)

        Property Owner As IEvaluableNode

    End Interface

End Namespace
