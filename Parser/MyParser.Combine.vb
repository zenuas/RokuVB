﻿Imports Roku.Node


Namespace Parser

    Partial Public Class MyParser

        Public Overridable Property Loader As Loader

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

        Protected Overridable Sub AddUse(use As UseNode)

            CType(Me.CurrentScope, ProgramNode).Uses.Add(use)
        End Sub

        Protected Overridable Function CreateLetNode(
                var As VariableNode,
                expr As IEvaluableNode,
                binding As Boolean
            ) As LetNode

            Dim let_ As New LetNode With {.Var = var, .Expression = expr, .NameBinding = binding}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Protected Overridable Function CreateLetNode(
                prop As PropertyNode,
                expr As IEvaluableNode
            ) As LetNode

            Dim let_ As New LetNode With {.Receiver = prop.Left, .Var = prop.Right, .Expression = expr}
            let_.AppendLineNumber(prop)
            Return let_
        End Function

        Protected Overridable Function CreateLetNode(
                var As VariableNode,
                type As TypeNode
            ) As LetNode

            Dim let_ As New LetNode With {.Var = var, .Declare = type}
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

        Protected Overridable Function CreateListNode(Of T)() As ListNode(Of T)

            Return New ListNode(Of T)
        End Function

        Protected Overridable Function CreateListNode(Of T)(expr As T) As ListNode(Of T)

            Dim list = Me.CreateListNode(Of T)
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
                [then] As BlockNode
            ) As IfNode

            Return Me.CreateIfNode(cond, [then], Nothing)
        End Function

        Protected Overridable Function CreateIfNode(
                cond As IEvaluableNode,
                [then] As BlockNode,
                [else] As BlockNode
            ) As IfNode

            Dim [if] As New IfNode With {.Condition = cond, .Then = [then], .Else = [else]}
            [if].AppendLineNumber(cond)
            Return [if]
        End Function

        Protected Overridable Function CreateFunctionNode(
                name As VariableNode,
                args() As DeclareNode,
                ret As TypeNode,
                body As BlockNode
            ) As FunctionNode

            Dim f As New FunctionNode(name.Name) With {.Arguments = args, .Return = ret, .Body = body}
            body.Owner = f
            f.AppendLineNumber(name)
            Return f
        End Function

        Protected Overridable Function CreateFunctionTypeNode(
                args() As TypeNode,
                ret As TypeNode,
                token As Token
            ) As TypeFunctionNode

            Dim t As New TypeFunctionNode
            t.Arguments = args
            t.Return = ret
            t.AppendLineNumber(token)
            Return t
        End Function

        Protected Overridable Function ToBlock(if_ As IfNode) As BlockNode

            Dim block = New BlockNode(if_.LineNumber.Value)
            if_.Parent = block
            block.Statements.Add(if_)
            block.Parent = Me.CurrentScope
            Return block
        End Function

    End Class

End Namespace
