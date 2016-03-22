
%{
Imports Roku.Node
Imports VariableListNode = Roku.Node.ListNode(Of Roku.Node.VariableNode)
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
Imports TypeListNode = Roku.Node.ListNode(Of Roku.Node.TypeNode)
Imports IEvaluableListNode = Roku.Node.ListNode(Of Roku.Node.IEvaluableNode)
%}

%default INode
%define YYROOTNAMESPACE Roku
%define YYNAMESPACE     Parser
%define YYMYNAMESPACE   Parser

%type<BlockNode>      block stmt
%type<IEvaluableNode> line
%type<LetNode>        let
%type<FunctionNode>   sub
%type<DeclareNode>    decla
%type<DeclareListNode> args argn
%type<TypeNode>       type typex
%type<TypeListNode>   types typen
%type<IfNode>         if ifthen elseif
%type<StructNode>     struct struct_block
%type<IEvaluableNode> expr
%type<IEvaluableNode> call
%type<IEvaluableListNode> list listn
%type<VariableNode>   var varx atvar
%type<VariableListNode> atvarn
%type<NumericNode>    num
%type<StringNode>     str
%type<UseNode>        use
%type<IEvaluableNode> namespace

%left  ELSE
%token<NumericNode>  NUM
%left  OPE
%left  '.'
%left  ALLOW

%left  '?'
%right '(' '['

%%

start : program_begin uses stmt END {$$ = Me.PopScope}
program_begin : BEGIN               {Me.PushScope(New ProgramNode)}

uses  : void
      | uses use EOL           {Me.AddUse($2)}
use   : USE namespace          {$$ = Me.AppendLineNumber(New UseNode With {.Namespace = $2}, $1)}
      | USE var EQ namespace   {$$ = Me.AppendLineNumber(New UseNode With {.Namespace = $4, .Alias = $2.Name}, $1)}

namespace : varx               {$$ = $1}
          | namespace '.' varx {$$ = Me.CreateExpressionNode($1, ".", $3)}


########## statement ##########
stmt  : void        {$$ = Me.CurrentScope}
      | stmt line   {$1.AddStatement($2) : $$ = $1}

line  : expr EOL
      | let  EOL
      | sub         {Me.CurrentScope.AddFunction($1)}
      | if
      | block
      | struct      {Me.CurrentScope.Scope.Add($1.Name, $1)}

block : begin stmt END {$$ = Me.PopScope}
begin : BEGIN          {Me.PushScope(New BlockNode($1.LineNumber.Value))}


########## expr ##########
expr : var
     | str
     | num
     | call
     | lambda
     | '[' list ']'      {$$ = $2}
     | '(' expr ')'      {$$ = Me.CreateExpressionNode($2, "()")}
#     | OPE expr          {$$ = Me.CreateExpressionNode($2, $1.Name)}
     | expr '.' varx     {$$ = New PropertyNode With {.Left = $1, .Right = $3}}
     | expr OPE expr     {$$ = Me.CreateExpressionNode($1, $2.Name, $3)}
     | expr '[' expr ']' {$$ = Me.CreateExpressionNode($1, "[]", $3)}
     | expr '?' expr ':' expr

call : expr '(' list ')' {$$ = New FunctionCallNode($1, $3.List.ToArray)}

list  : void             {$$ = Me.CreateListNode(Of IEvaluableNode)}
      | listn extra
listn : expr             {$$ = Me.CreateListNode($1)}
      | listn ',' expr   {$1.List.Add($3) : $$ = $1}


########## let ##########
let : LET var EQ expr       {$$ = Me.CreateLetNode($2, $4, True)}
    | var EQ expr           {$$ = Me.CreateLetNode($1, $3, False)}
    | expr '.' varx EQ expr {$$ = Me.CreateLetNode(New PropertyNode With {.Left = $1, .Right = $3}, $5)}


########## struct ##########
struct : STRUCT var EOL struct_block                {$4.Name = $2.Name : $$ = $4}
       | STRUCT var '(' atvarn ')' EOL struct_block {$7.Name = $2.Name : $7.Generics.AddRange($4.List) : $$ = $7}

struct_block : struct_begin define END {$$ = Me.PopScope}
struct_begin : BEGIN                   {Me.PushScope(New StructNode($1.LineNumber.Value))}

define : void
       | define LET var ':' type EOL   {Me.CurrentScope.AddLet(Me.CreateLetNode($3, $5))}
       | define LET var EQ  expr EOL   {Me.CurrentScope.AddLet(Me.CreateLetNode($3, $5, True))}
       | define sub

atvarn : atvar                         {$$ = Me.CreateListNode($1)}
       | atvarn ',' atvar              {$1.List.Add($3) : $$ = $1}


########## sub ##########
sub   : SUB var '(' args ')' typex EOL block {$$ = Me.CreateFunctionNode($2, $4.List.ToArray, $6, $8)}
args  : void           {$$ = Me.CreateListNode(Of DeclareNode)}
      | argn extra
argn  : decla          {$$ = Me.CreateListNode($1)}
      | argn ',' decla {$1.List.Add($3) : $$ = $1}
decla : var ':' type   {$$ = New DeclareNode($1, $3)}
type  : var            {$$ = New TypeNode($1)}
      | '[' type ']'   {$2.IsArray = True : $$ = $2}
      | atvar          {$$ = New TypeNode($1) With {.IsGeneric = True}}
      | '{' types '}'            {$$ = CreateFunctionTypeNode($2.List.ToArray, Nothing, $1)}
      | '{' types '}' ALLOW type {$$ = CreateFunctionTypeNode($2.List.ToArray, $5,      $1)}
typex : void
      | type
types : void           {$$ = Me.CreateListNode(Of TypeNode)}
      | typen extra
typen : type           {$$ = Me.CreateListNode($1)}
      | typen ',' type {$1.List.Add($3) : $$ = $1}


########## lambda ##########
lambda      : '{' lambda_args '}' ALLOW lambda_func
lambda_func : expr
            | block
lambda_arg  : var
lambda_args : void
            | lambda_argn extra
lambda_argn : lambda_arg
            | lambda_argn ',' lambda_arg


########## if ##########
if     : ifthen
       | elseif
       | ifthen ELSE EOL block {$1.Else = $4 : $$ = $1}
       | elseif ELSE EOL block {$1.Else = $4 : $$ = $1}
ifthen : IF expr EOL block     {$$ = Me.CreateIfNode($2, $4)}
elseif : ifthen ELSE ifthen    {$1.Else = Me.ToBlock($3) : $$ = $1}
       | elseif ELSE ifthen    {$1.Else = Me.ToBlock($3) : $$ = $1}


########## other ##########
var   : VAR     {$$ = Me.CreateVariableNode($1)}
varx  : var
      | SUB     {$$ = Me.CreateVariableNode($1)}
      | IF      {$$ = Me.CreateVariableNode($1)}
      | ELSE    {$$ = Me.CreateVariableNode($1)}
      | LET     {$$ = Me.CreateVariableNode($1)}
      | USE     {$$ = Me.CreateVariableNode($1)}
atvar : ATVAR   {$$ = Me.CreateVariableNode($1)}
num   : NUM     {$$ = $1}
str   : STR     {$$ = New StringNode($1)}
      | str STR {$1.String.Append($2.Name) : $$ = $1}

extra : void
      | ','

void  : {$$ = Nothing}

