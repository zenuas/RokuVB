Imports System.Collections.Generic


Namespace Node

    Public Interface IScopeNode
        Inherits INode

        ReadOnly Property Scope As Dictionary(Of String, INode)
        Sub AddLet(let_ As LetNode)

        Property Owner As IBlock
        Property InnerScope As Boolean

    End Interface

End Namespace
