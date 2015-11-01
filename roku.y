
%{
Imports Roku.Node
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
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
%type<IfNode>         if ifthen elseif
%type<IEvaluableNode> expr
%type<IEvaluableNode> call
%type<IEvaluableListNode> list
%type<VariableNode>   var varx
%type<NumericNode>    num
%type<StringNode>     str

%left  ELSE
%token<NumericNode> NUM
%left  OPE

%left  '?'
%right '(' '['

%%

start : block


########## statement ##########
stmt  : void        {$$ = Me.CurrentScope}
      | stmt line   {$1.AddStatement($2) : $$ = $1}

line  : expr EOL
      | call EOL
      | let  EOL
      | sub         {Me.CurrentScope.AddFunction($1)}
      | if
      | block
      | struct

block : begin stmt END {$$ = Me.PopScope}
begin : BEGIN          {Me.PushScope(New BlockNode($1.LineNumber.Value))}


########## expr ##########
expr : var
     | str
     | num
     | '(' expr ')'      {$$ = Me.CreateExpressionNode($2, "()")}
     | '(' call ')'      {$$ = Me.CreateExpressionNode($2, "()")}
#     | OPE expr          {$$ = Me.CreateExpressionNode($2, $1.Name)}
     | expr OPE expr     {$$ = Me.CreateExpressionNode($1, $2.Name, $3)}
     | expr '[' expr ']' {$$ = Me.CreateExpressionNode($1, "[]", $3)}
     | expr '?' expr ':' expr

call : expr list         {$$ = New FunctionCallNode($1, $2.List.ToArray)}

list : expr              {$$ = Me.CreateListNode($1)}
     | list expr         {$1.List.Add($2) : $$ = $1}


########## let ##########
let : LET var EQ expr    {$$ = Me.CreateLetNode($2, $4)}


########## struct ##########
struct : STRUCT var EOL block


########## sub ##########
sub   : SUB var '(' args ')' typex EOL block {$$ = Me.CreateFunctionNode($2, $4.List.ToArray, $6, $8)}
args  : void       {$$ = Me.CreateListNode(Of DeclareNode)}
      | argn
argn  : decla      {$$ = Me.CreateListNode($1)}
      | argn decla {$1.List.Add($2) : $$ = $1}
decla : var ':' type {$$ = New DeclareNode($1, $3)}
type  : var          {$$ = New TypeNode($1)}
      | '[' type ']'
typex : void
      | type


########## if ##########
if     : ifthen
       | elseif
       | if ELSE EOL block  {$1.Else = $4 : $$ = $1}
ifthen : IF expr EOL block  {$$ = Me.CreateIfNode($2, $4)}
elseif : ifthen ELSE ifthen {$1.Else = $3 : $$ = $1}
       | elseif ELSE ifthen {$1.Else = $3 : $$ = $1}

########## other ##########
var   : VAR     {$$ = Me.CreateVariableNode($1)}
varx  : var
      | SUB     {$$ = Me.CreateVariableNode($1)}
      | IF      {$$ = Me.CreateVariableNode($1)}
      | ELSE    {$$ = Me.CreateVariableNode($1)}
      | LET     {$$ = Me.CreateVariableNode($1)}
num   : NUM     {$$ = $1}
str   : STR     {$$ = New StringNode($1)}
      | str STR {$1.String.Append($2.Name) : $$ = $1}

void : {$$ = Nothing}

