Imports System.Collections.Generic
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

        Public Shared Function AppendLineNumber(Of T As BaseNode)(node As T, token As Token) As T

            node.AppendLineNumber(token)
            Return node
        End Function

        Public Shared Sub AddUse(parser As MyParser, use As UseNode)

            use.Module = parser.Loader.AddUse(use.GetNamespace)
            CType(parser.CurrentScope, ProgramNode).Uses.Add(use)
        End Sub

        Public Shared Function CreateLetNode(
                var As VariableNode,
                expr As IEvaluableNode,
                Optional local_var As Boolean = False
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Expression = expr}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Shared Function CreateLetNode(
                prop As PropertyNode,
                expr As IEvaluableNode
            ) As LetNode

            Dim let_ As New LetNode With {.Receiver = prop.Left, .Var = prop.Right, .Expression = expr}
            let_.AppendLineNumber(prop)
            Return let_
        End Function

        Public Shared Function CreateLetNode(
                var As VariableNode,
                type As TypeBaseNode,
                Optional local_var As Boolean = False
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Declare = type}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Shared Function CreateLetNode(
                var As VariableNode,
                type As TypeBaseNode,
                expr As IEvaluableNode,
                Optional local_var As Boolean = False
            ) As LetNode

            var.LocalVariable = local_var
            Dim let_ As New LetNode With {.Var = var, .Declare = type, .Expression = expr}
            let_.AppendLineNumber(var)
            Return let_
        End Function

        Public Shared Function CreateFunctionCallNode(
                expr As IEvaluableNode,
                ParamArray args() As IEvaluableNode
            ) As FunctionCallNode

            Dim fcall As New FunctionCallNode With {.Expression = expr, .Arguments = args}
            fcall.AppendLineNumber(expr)
            Return fcall
        End Function

        Public Shared Function CreateFunctionCallNode(
                ope As Token,
                ParamArray args() As IEvaluableNode
            ) As FunctionCallNode

            Dim expr = CreateVariableNode(ope)
            expr.AppendLineNumber(args(0))
            Return CreateFunctionCallNode(expr, args)
        End Function

        Public Shared Function CreatePropertyNode(
                left As IEvaluableNode,
                dot As Token,
                right As VariableNode
            ) As PropertyNode

            Dim prop As New PropertyNode With {.Left = left, .Right = right}
            prop.AppendLineNumber(dot)
            Return prop
        End Function

        Public Shared Function CreateExpressionNode(
                left As IEvaluableNode,
                ope As String,
                right As IEvaluableNode
            ) As ExpressionNode

            Dim expr As New ExpressionNode With {.Left = left, .Operator = ope, .Right = right}
            expr.AppendLineNumber(left)
            Return expr
        End Function

        Public Shared Function CreateExpressionNode(
                left As IEvaluableNode,
                ope As String
            ) As ExpressionNode

            Return CreateExpressionNode(left, ope, Nothing)
        End Function

        Public Shared Function CreateExpressionNode(
                left As IEvaluableNode
            ) As ExpressionNode

            If TypeOf left Is ExpressionNode Then Return CType(left, ExpressionNode)
            Return CreateExpressionNode(left, "", Nothing)
        End Function

        Public Shared Function CreateTupleNode(
                items As ListNode(Of IEvaluableNode)
            ) As TupleNode

            Dim tuple As New TupleNode With {.Items = items.List.ToArray}
            tuple.AppendLineNumber(items)
            Return tuple
        End Function

        Public Shared Function CreateListNode(Of T As INode)() As ListNode(Of T)

            Return New ListNode(Of T)
        End Function

        Public Function CreateListNode(Of T As INode)(ParamArray expr() As T) As ListNode(Of T)

            Dim list = CreateListNode(Of T)()
            list.List.AddRange(expr)

            Return list
        End Function

        Public Shared Function CreateVariableNode(s As Token) As VariableNode

            Return CreateVariableNode(s.Name, s)
        End Function

        Public Shared Function CreateVariableNode(s As String, pos As Token) As VariableNode

            If TypeOf pos.Value Is VariableNode Then Return CType(pos.Value, VariableNode)

            Dim var_ = New VariableNode(s)
            var_.AppendLineNumber(pos)

            Return var_
        End Function

        Public Shared Function CreateVariableNode(s As String, node As INode) As VariableNode

            Dim var_ = New VariableNode(s)
            var_.AppendLineNumber(node)

            Return var_
        End Function

        Public Shared Function CreateIfNode(
                cond As IEvaluableNode,
                [then] As BlockNode
            ) As IfNode

            Return CreateIfNode(cond, [then], Nothing)
        End Function

        Public Shared Function CreateIfNode(
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

        Public Shared Function CreateIfCastNode(
                var As VariableNode,
                decla As TypeBaseNode,
                cond As IEvaluableNode,
                [then] As BlockNode
            ) As IfCastNode

            If [then] IsNot Nothing Then [then].InnerScope = True
            Dim [if] As New IfCastNode With {.Condition = cond, .Then = [then], .Var = var, .Declare = decla}
            [if].AppendLineNumber(var)
            Return [if]
        End Function

        Public Shared Function AddElse(
                [if] As IfNode,
                [else] As BlockNode
            ) As IfNode

            If [if].Else Is Nothing Then

                [if].Else = [else]
            Else

                AddElse(CType([if].Else.Statements(0), IfNode), [else])
            End If
            Return [if]
        End Function

        Public Shared Function CreateSwitchNode([case] As CaseNode) As SwitchNode

            Dim switch As New SwitchNode
            switch.Case.Add([case])
            switch.AppendLineNumber([case])
            Return switch
        End Function

        Public Shared Function CreateCaseCastNode(
                decla As TypeBaseNode,
                var As VariableNode
            ) As CaseCastNode

            Dim [case] As New CaseCastNode With {.Declare = decla, .Var = var}
            [case].AppendLineNumber(decla)
            Return [case]
        End Function

        Public Shared Function CreateCaseArrayNode(
                pattern As ListNode(Of VariableNode),
                token As Token
            ) As CaseArrayNode

            Dim [case] As New CaseArrayNode With {.Pattern = pattern.List}
            [case].AppendLineNumber(token)
            Return [case]
        End Function

        Public Shared Function CreateClassNode(
                name As VariableNode,
                args As ListNode(Of TypeBaseNode),
                conds As ListNode(Of FunctionNode)
            ) As ClassNode

            Dim class_ As New ClassNode(name.LineNumber.Value)
            class_.Name = name.Name
            class_.Generics.AddRange(args.List)
            conds.List.Each(Sub(x) class_.AddFunction(x))
            Return class_
        End Function

        Public Shared Function CreateFunctionNode(
                f As FunctionNode,
                name As VariableNode,
                args As ListNode(Of DeclareNode),
                ret As TypeBaseNode
            ) As FunctionNode

            f.Name = name.Name
            f.Arguments = args.List
            f.Return = ret
            f.InnerScope = False
            Return f
        End Function

        Public Shared Function CreateFunctionNode(
                f As FunctionNode,
                args As ListNode(Of DeclareNode),
                ret As TypeBaseNode
            ) As FunctionNode

            args.List.Each(Sub(x, i) If x.Type Is Nothing Then x.Type = New TypeNode With {.Name = $"#{i}", .IsGeneric = True})
            f.Name = $"#{f.LineNumber},{f.LineColumn}"
            f.Arguments = args.List
            f.Return = ret
            f.InnerScope = False
            Return f
        End Function

        Public Shared Function CreateFunctionNode(
                name As VariableNode,
                args As ListNode(Of DeclareNode),
                ret As TypeBaseNode
            ) As FunctionNode

            Return CreateFunctionNode(name, args, ret)
        End Function

        Public Shared Function CreateFunctionNode(
                name As VariableNode,
                args As ListNode(Of TypeBaseNode),
                ret As TypeBaseNode
            ) As FunctionNode

            Return CreateFunctionNode(name, args.List.Map(Function(x, i) New DeclareNode(CreateVariableNode($"arg{i}", x), x)).ToList, ret)
        End Function

        Public Shared Function CreateFunctionNode(
                name As VariableNode,
                args As List(Of DeclareNode),
                ret As TypeBaseNode
            ) As FunctionNode

            Dim f As New FunctionNode(name.LineNumber.Value)
            f.Name = name.Name
            f.Arguments = args
            f.Return = ret
            f.InnerScope = False
            Return f
        End Function

        Public Shared Function CreateFunctionTypeNode(
                args As ListNode(Of TypeBaseNode),
                ret As TypeBaseNode,
                token As Token
            ) As TypeFunctionNode

            Dim t As New TypeFunctionNode
            t.Arguments = args.List
            t.Return = ret
            t.AppendLineNumber(token)
            Return t
        End Function

        Public Shared Function CreateLambdaFunction(
                scope As IScopeNode,
                f As FunctionNode,
                args As ListNode(Of DeclareNode),
                ret As TypeBaseNode
            ) As VariableNode

            f = CreateFunctionNode(f, If(args, New ListNode(Of DeclareNode)), ret)
            Dim v = New VariableNode(f.Name)
            v.AppendLineNumber(f)
            scope.Lets.Add(f.Name, f)
            Return v
        End Function

        Public Shared Function CreateImplicitLambdaFunction(
                scope As IScopeNode,
                f As FunctionNode,
                args As ListNode(Of DeclareNode),
                ret As TypeNode
            ) As VariableNode

            f = CreateFunctionNode(f, If(args, New ListNode(Of DeclareNode)), ret)
            If args Is Nothing OrElse args.List.Count = 0 Then f.ImplicitArgumentsCount = 0
            If ret Is Nothing Then f.ImplicitReturn = (f.Statements.Count > 0 AndAlso TypeOf f.Statements(0) Is LambdaExpressionNode)
            Dim v = New VariableNode(f.Name)
            v.AppendLineNumber(f)
            scope.Lets.Add(f.Name, f)
            Return v
        End Function

        Public Shared Function ToLambdaExpression(scope As IScopeNode, expr As IEvaluableNode) As FunctionNode

            Dim f = New FunctionNode(expr.LineNumber.Value)
            Dim lambda = New LambdaExpressionNode With {.Expression = expr}
            f.AppendLineNumber(expr)
            lambda.AppendLineNumber(expr)
            f.Statements.Add(lambda)
            f.Parent = scope
            Return f
        End Function

        Public Shared Function ToLambdaExpressionBlock(scope As IScopeNode, expr As IEvaluableNode) As BlockNode

            Dim block = New BlockNode(expr.LineNumber.Value)
            Dim lambda = New LambdaExpressionNode With {.Expression = expr}
            lambda.AppendLineNumber(expr)
            block.Statements.Add(lambda)
            block.Parent = scope
            Return block
        End Function

        Public Shared Function ToBlock(scope As IScopeNode, stmt As IStatementNode) As BlockNode

            Dim block = New BlockNode(stmt.LineNumber.Value)
            block.Statements.Add(stmt)
            block.Parent = scope
            Return block
        End Function

    End Class

End Namespace
