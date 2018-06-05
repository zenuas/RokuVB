Imports Roku.Node
Imports Roku.Util.Extensions


Namespace Parser

    Partial Public Class MyParser

        Public Overridable Property Loader As Loader

#Region "scope"

        Public Overridable Property CurrentScope As IScopeNode

        Public Overridable Sub PushScope(scope As IScopeNode)

            scope.Parent = Me.CurrentScope
            Me.CurrentScope = scope
        End Sub

        Public Overridable Function PopScope() As IScopeNode

            Dim prev_scope = Me.CurrentScope
            Me.CurrentScope = Me.CurrentScope.Parent
            Return prev_scope
        End Function

#End Region

        Public Overridable Function AppendLineNumber(Of T As BaseNode)(node As T, token As Token) As T

            node.AppendLineNumber(token)
            Return node
        End Function

        Public Overridable Sub AddUse(use As UseNode)

            use.Module = Me.Loader.AddUse(use.GetNamespace)
            CType(Me.CurrentScope, ProgramNode).Uses.Add(use)
        End Sub

        Public Overridable Function CreateLetNode(
                var As VariableNode,
                expr As IEvaluableNode,
                Optional local_var As Boolean = False
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Expression = expr}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Overridable Function CreateLetNode(
                prop As PropertyNode,
                expr As IEvaluableNode
            ) As LetNode

            Dim let_ As New LetNode With {.Receiver = prop.Left, .Var = prop.Right, .Expression = expr}
            let_.AppendLineNumber(prop)
            Return let_
        End Function

        Public Overridable Function CreateLetNode(
                var As VariableNode,
                type As TypeNode,
                Optional local_var As Boolean = False
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Declare = type}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Overridable Function CreateLetNode(
                var As VariableNode,
                type As TypeNode,
                expr As IEvaluableNode,
                Optional local_var As Boolean = False
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Declare = type, .Expression = expr}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Overridable Function CreateFunctionCallNode(
                expr As IEvaluableNode,
                ParamArray args() As IEvaluableNode
            ) As FunctionCallNode

            Dim fcall As New FunctionCallNode With {.Expression = expr, .Arguments = args}
            fcall.AppendLineNumber(expr)
            Return fcall
        End Function

        Public Overridable Function CreateFunctionCallNode(
                ope As Token,
                ParamArray args() As IEvaluableNode
            ) As FunctionCallNode

            Dim expr = Me.CreateVariableNode(ope)
            expr.AppendLineNumber(args(0))
            Return Me.CreateFunctionCallNode(expr, args)
        End Function

        Public Overridable Function CreatePropertyNode(
                left As IEvaluableNode,
                dot As Token,
                right As VariableNode
            ) As PropertyNode

            Dim prop As New PropertyNode With {.Left = left, .Right = right}
            prop.AppendLineNumber(dot)
            Return prop
        End Function

        Public Overridable Function CreateExpressionNode(
                left As IEvaluableNode,
                ope As String,
                right As IEvaluableNode
            ) As ExpressionNode

            Dim expr As New ExpressionNode With {.Left = left, .Operator = ope, .Right = right}
            expr.AppendLineNumber(left)
            Return expr
        End Function

        Public Overridable Function CreateExpressionNode(
                left As IEvaluableNode,
                ope As String
            ) As ExpressionNode

            Return Me.CreateExpressionNode(left, ope, Nothing)
        End Function

        Public Overridable Function CreateExpressionNode(
                left As IEvaluableNode
            ) As ExpressionNode

            If TypeOf left Is ExpressionNode Then Return CType(left, ExpressionNode)
            Return Me.CreateExpressionNode(left, "", Nothing)
        End Function

        Public Overridable Function CreateTupleNode(
                items As ListNode(Of IEvaluableNode)
            ) As TupleNode

            Dim tuple As New TupleNode With {.Items = items.List.ToArray}
            tuple.AppendLineNumber(items)
            Return tuple
        End Function

        Public Overridable Function CreateListNode(Of T As INode)() As ListNode(Of T)

            Return New ListNode(Of T)
        End Function

        Public Overridable Function CreateListNode(Of T As INode)(ParamArray expr() As T) As ListNode(Of T)

            Dim list = Me.CreateListNode(Of T)
            list.List.AddRange(expr)

            Return list
        End Function

        Public Overridable Function CreateVariableNode(s As Token) As VariableNode

            Return Me.CreateVariableNode(s.Name, s)
        End Function

        Public Overridable Function CreateVariableNode(s As String, pos As Token) As VariableNode

            Dim var_ = New VariableNode(s)
            var_.AppendLineNumber(pos)

            Return var_
        End Function

        Public Overridable Function CreateVariableNode(s As String, node As INode) As VariableNode

            Dim var_ = New VariableNode(s)
            var_.AppendLineNumber(node)

            Return var_
        End Function

        Public Overridable Function CreateIfNode(
                cond As IEvaluableNode,
                [then] As BlockNode
            ) As IfNode

            Return Me.CreateIfNode(cond, [then], Nothing)
        End Function

        Public Overridable Function CreateIfNode(
                cond As IEvaluableNode,
                [then] As BlockNode,
                [else] As BlockNode
            ) As IfNode

            If [then] IsNot Nothing Then [then].InnerScope = True
            If [else] IsNot Nothing Then [else].InnerScope = True
            Dim [if] As New IfNode With {.Condition = cond, .Then = [then], .Else = [else]}
            [if].AppendLineNumber(cond)
            Return [if]
        End Function

        Public Overridable Function CreateIfCastNode(
                var As VariableNode,
                decla As TypeNode,
                cond As IEvaluableNode,
                [then] As BlockNode
            ) As IfCastNode

            If [then] IsNot Nothing Then [then].InnerScope = True
            Dim [if] As New IfCastNode With {.Condition = cond, .Then = [then], .Var = var, .Declare = decla}
            [if].AppendLineNumber(var)
            Return [if]
        End Function

        Public Overridable Function AddElse(
                [if] As IfNode,
                [else] As BlockNode
            ) As IfNode

            If [if].Else Is Nothing Then

                [if].Else = [else]
            Else

                Me.AddElse(CType([if].Else.Statements(0), IfNode), [else])
            End If
            Return [if]
        End Function

        Public Overridable Function CreateSwitchNode([case] As CaseNode) As SwitchNode

            Dim switch As New SwitchNode
            switch.Case.Add([case])
            switch.AppendLineNumber([case])
            Return switch
        End Function

        Public Overridable Function CreateCaseCastNode(
                decla As TypeNode,
                var As VariableNode
            ) As CaseCastNode

            Dim [case] As New CaseCastNode With {.Declare = decla, .Var = var}
            [case].AppendLineNumber(decla)
            Return [case]
        End Function

        Public Overridable Function CreateCaseArrayNode(
                pattern As ListNode(Of VariableNode),
                token As Token
            ) As CaseArrayNode

            Dim [case] As New CaseArrayNode With {.Pattern = pattern.List}
            [case].AppendLineNumber(token)
            Return [case]
        End Function

        Public Overridable Function CreateFunctionNode(
                f As FunctionNode,
                name As VariableNode,
                args() As DeclareNode,
                ret As TypeNode
            ) As FunctionNode

            f.Name = name.Name
            f.Arguments = args
            f.Return = ret
            f.InnerScope = False
            Return f
        End Function

        Public Overridable Function CreateFunctionNode(
                f As FunctionNode,
                args() As DeclareNode,
                ret As TypeNode
            ) As FunctionNode

            args.Each(Sub(x, i) If x.Type Is Nothing Then x.Type = New TypeNode With {.Name = $"#{i}", .IsGeneric = True})
            f.Name = $"#{f.LineNumber},{f.LineColumn}"
            f.Arguments = args
            f.Return = ret
            f.InnerScope = False
            Return f
        End Function

        Public Overridable Function CreateFunctionTypeNode(
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

        Public Overridable Function CreateLambdaFunction(
                f As FunctionNode,
                args() As DeclareNode,
                ret As TypeNode
            ) As VariableNode

            f = Me.CreateFunctionNode(f, If(args, New DeclareNode() {}), ret)
            Dim v = New VariableNode(f.Name)
            v.AppendLineNumber(f)
            Me.CurrentScope.Lets.Add(f.Name, f)
            Return v
        End Function

        Public Overridable Function CreateImplicitLambdaFunction(
                f As FunctionNode,
                args() As DeclareNode,
                ret As TypeNode
            ) As VariableNode

            f = Me.CreateFunctionNode(f, If(args, New DeclareNode() {}), ret)
            If args Is Nothing OrElse args.Length = 0 Then f.ImplicitArguments = True
            If ret Is Nothing Then f.ImplicitReturn = True
            Dim v = New VariableNode(f.Name)
            v.AppendLineNumber(f)
            Me.CurrentScope.Lets.Add(f.Name, f)
            Return v
        End Function

        Public Overridable Function ToLambdaExpression(expr As IEvaluableNode) As FunctionNode

            Dim f = New FunctionNode(expr.LineNumber.Value)
            Dim lambda = New LambdaExpressionNode With {.Expression = expr}
            lambda.AppendLineNumber(expr)
            f.Statements.Add(lambda)
            f.Parent = Me.CurrentScope
            Return f
        End Function

        Public Overridable Function ToLambdaExpressionBlock(expr As IEvaluableNode) As BlockNode

            Dim block = New BlockNode(expr.LineNumber.Value)
            Dim lambda = New LambdaExpressionNode With {.Expression = expr}
            lambda.AppendLineNumber(expr)
            block.Statements.Add(lambda)
            block.Parent = Me.CurrentScope
            Return block
        End Function

        Public Overridable Function ToBlock(if_ As IfNode) As BlockNode

            Dim block = New BlockNode(if_.LineNumber.Value)
            block.Statements.Add(if_)
            block.Parent = Me.CurrentScope
            Return block
        End Function

    End Class

End Namespace
