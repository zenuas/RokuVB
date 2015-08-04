Imports Roku.Node


Namespace Compiler

    Partial Public Class MyParser

#Region "scope"

        Protected Overridable Property CurrentScope As IScopeNode

        Protected Overridable Sub PushScope(scope As IScopeNode)

            scope.Parent = Me.CurrentScope
            Me.CurrentScope = scope
        End Sub

        Protected Overridable Function PopScope() As IScopeNode

            Dim prev_scope = Me.CurrentScope
            Me.CurrentScope = Me.CurrentScope.Parent
            Return prev_scope
        End Function

#End Region

        Protected Overridable Function CreateLetNode(
                var As VariableNode,
                expr As IEvaluableNode
            ) As LetNode

            Dim let_ As New LetNode With {.Var = var, .Expression = expr}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Protected Overridable Function CreateExpressionNode(
                left As IEvaluableNode,
                ope As String,
                right As IEvaluableNode
            ) As ExpressionNode

            Dim expr As New ExpressionNode With {.Left = left, .Operator = ope, .Right = right}
            expr.AppendLineNumber(left)
            Return expr
        End Function

        Protected Overridable Function CreateExpressionNode(
                left As IEvaluableNode,
                ope As String
            ) As ExpressionNode

            Return Me.CreateExpressionNode(left, ope, Nothing)
        End Function

        Protected Overridable Function CreateExpressionNode(
                left As IEvaluableNode
            ) As ExpressionNode

            If TypeOf left Is ExpressionNode Then Return CType(left, ExpressionNode)
            Return Me.CreateExpressionNode(left, "", Nothing)
        End Function

        Protected Overridable Function CreateListNode() As ListNode

            Return New ListNode
        End Function

        Protected Overridable Function CreateListNode(expr As IEvaluableNode) As ListNode

            Dim list = Me.CreateListNode
            list.List.Add(expr)

            Return list
        End Function

        Protected Overridable Function CreateVariableNode(s As String) As VariableNode

            Return New VariableNode(s)
        End Function

        Protected Overridable Function CreateVariableNode(s As Token) As VariableNode

            Dim var_ = Me.CreateVariableNode(s.Name)
            var_.AppendLineNumber(s)

            Return var_
        End Function

        Protected Overridable Function CreateIfNode(
                cond As IEvaluableNode,
                [then] As IEvaluableNode
            ) As IfNode

            Return Me.CreateIfNode(cond, [then], Nothing)
        End Function

        Protected Overridable Function CreateIfNode(
                cond As IEvaluableNode,
                [then] As IEvaluableNode,
                [else] As IEvaluableNode
            ) As IfNode

            Dim [if] As New IfNode With {.Condition = cond, .Then = [then], .Else = [else]}
            [if].AppendLineNumber(cond)
            Return [if]
        End Function

    End Class

End Namespace
