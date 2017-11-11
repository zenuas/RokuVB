Imports System.Collections.Generic


Namespace Node

    Public Interface IScopeNode
        Inherits INode

        Property Parent As IScopeNode
        ReadOnly Property Scope As Dictionary(Of String, INode)
        Sub AddLet(let_ As LetNode)

        Property Owner As INamedFunction
        Property InnerScope As Boolean

    End Interface

End Namespace
