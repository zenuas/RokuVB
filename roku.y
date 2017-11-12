
%{
Imports Roku.Node
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
Imports TypeListNode = Roku.Node.ListNode(Of Roku.Node.TypeNode)
Imports VariableListNode = Roku.Node.ListNode(Of Roku.Node.VariableNode)
Imports IEvaluableListNode = Roku.Node.ListNode(Of Roku.Node.IEvaluableNode)
%}

%default INode
%define YYROOTNAMESPACE Roku
%define YYNAMESPACE     Parser
%define YYMYNAMESPACE   Parser

%type<BlockNode>      block stmt lambda_func
%type<IStatementNode> line
%type<LetNode>        let
%type<FunctionNode>   sub
%type<DeclareNode>    decla lambda_arg
%type<DeclareListNode> args argn lambda_args lambda_argn
%type<TypeNode>       type typex atvar union
%type<TypeListNode>   types typen atvarn unionn typeor
%type<IfNode>         if ifthen elseif
%type<SwitchNode>     switch casen case_block
%type<CaseNode>       case case_expr
%type<StructNode>     struct struct_block
%type<IEvaluableNode> expr
%type<IEvaluableNode> call
%type<IEvaluableListNode> list listn
%type<VariableListNode>   patternn array_pattern
%type<VariableNode>   var varx fn pattern
%type<NumericNode>    num
%type<StringNode>     str
%type<UseNode>        use
%type<IEvaluableNode> namespace
%type<TokenNode>      ope

%left  VAR ATVAR STR NULL
%left  USE
%left  ELSE
%token<NumericNode> NUM
%left  OPE OR
%left  '.'
%left  ':'
%left  ARROW

%left  '?'
%right '(' '[' '{'
%left  EOL

%%

start :                             {$$ = New ProgramNode}
      | program_begin uses stmt END {$$ = Me.PopScope}
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

line  : call EOL
      | let  EOL
      | sub         {CType(Me.CurrentScope, IAddFunction).AddFunction($1)}
      | if
      | switch
      | block
      | struct      {Me.CurrentScope.Scope.Add($1.Name, $1)}
      | union       {Me.CurrentScope.Scope.Add($1.Name, $1)}

block : begin stmt END {$$ = Me.PopScope}
begin : BEGIN          {Me.PushScope(New BlockNode($1.LineNumber))}


########## expr ##########
expr : var
     | str
     | num
     | call
     | lambda
     | atvar
     | '[' list ']'      {$$ = $2}
     | '(' expr ')'      {$$ = Me.CreateExpressionNode($2, "()")}
#     | ope expr          {$$ = Me.CreateFunctionCallNode($1.Token, $2)}
     | expr '.' varx     {$$ = Me.CreatePropertyNode($1, $2, $3)}
     | expr ope expr     {$$ = Me.CreateFunctionCallNode($2.Token, $1, $3)}
     | expr '[' expr ']' {$$ = Me.CreateFunctionCallNode(Me.CreateVariableNode("[]", $2), $1, $3)}
     | expr '?' expr ':' expr
     | null

call : expr '(' list ')' {$$ = Me.CreateFunctionCallNode($1, $3.List.ToArray)}

list  : void             {$$ = Me.CreateListNode(Of IEvaluableNode)}
      | listn extra
listn : expr             {$$ = Me.CreateListNode($1)}
      | listn ',' expr   {$1.List.Add($3) : $$ = $1}


########## let ##########
let : LET var EQ expr          {$$ = Me.CreateLetNode($2, $4)}
    | LET var ':' type EQ expr {$$ = Me.CreateLetNode($2, $4, $6)}
#    | var EQ expr              {$$ = Me.CreateLetNode($1, $3)}
    | expr '.' varx EQ expr    {$$ = Me.CreateLetNode(Me.CreatePropertyNode($1, $2, $3), $5)}


########## struct ##########
struct : STRUCT var EOL struct_block                {$4.Name = $2.Name : $$ = $4}
       | STRUCT var '(' atvarn ')' EOL struct_block {$7.Name = $2.Name : $7.Generics.AddRange($4.List) : $$ = $7}

struct_block : struct_begin define END {$$ = Me.PopScope}
struct_begin : BEGIN                   {Me.PushScope(New StructNode($1.LineNumber))}

define : void
       | define LET var ':' type EOL   {Me.CurrentScope.AddLet(Me.CreateLetNode($3, $5))}
       | define LET var EQ  expr EOL   {Me.CurrentScope.AddLet(Me.CreateLetNode($3, $5))}
       | define sub

atvarn : atvar                         {$$ = Me.CreateListNode($1)}
       | atvarn ',' atvar              {$1.List.Add($3) : $$ = $1}


########## union ##########
union  : UNION var EOL BEGIN unionn END {$$ = New UnionNode($2, $5)}

unionn : type EOL        {$$ = Me.CreateListNode($1)}
       | unionn type EOL {$1.List.Add($2) : $$ = $1}


########## sub ##########
sub    : SUB fn '(' args ')' typex EOL block {$$ = Me.CreateFunctionNode($2, $4.List.ToArray, $6, $8)}
fn     : var
       | ope            {$$ = Me.CreateVariableNode($1.Token)}
args   : void           {$$ = Me.CreateListNode(Of DeclareNode)}
       | argn extra
argn   : decla          {$$ = Me.CreateListNode($1)}
       | argn ',' decla {$1.List.Add($3) : $$ = $1}
decla  : var ':' type   {$$ = New DeclareNode($1, $3)}
type   : var            {$$ = New TypeNode($1)}
       | var '?'        {$$ = New TypeNode($1) With {.Nullable = True}}
       | '[' type ']'   {$$ = New TypeArrayNode($2)}
       | '[' typeor ']' {$$ = New UnionNode($2)}
       | atvar
       | atvar '?'      {$$ = $1 : $1.Nullable = True}
       | '{' types '}'            {$$ = CreateFunctionTypeNode($2.List.ToArray, Nothing, $1)}
       | '{' types '}' ARROW type {$$ = CreateFunctionTypeNode($2.List.ToArray, $5,      $1)}
typex  : void
       | type
types  : void           {$$ = Me.CreateListNode(Of TypeNode)}
       | typen extra
typen  : type           {$$ = Me.CreateListNode($1)}
       | typen ',' type {$1.List.Add($3) : $$ = $1}
typeor : type OR type   {$$ = Me.CreateListNode($1, $3)}
       | typeor OR type {$1.List.Add($3) : $$ = $1}


########## lambda ##########
lambda      : '{' lambda_args '}' typex ARROW lambda_func {$$ = Me.CreateLambdaFunction($2.List.ToArray, $4, $6)}
lambda_func : expr                       {$$ = Me.ToLambdaExpression($1)}
            | block
lambda_arg  : var                        {$$ = New DeclareNode($1, Nothing)}
            | decla
lambda_args : void                       {$$ = Me.CreateListNode(Of DeclareNode)}
            | lambda_argn extra
lambda_argn : lambda_arg                 {$$ = Me.CreateListNode($1)}
            | lambda_argn ',' lambda_arg {$1.List.Add($3) : $$ = $1}


########## if ##########
if     : ifthen
       | elseif
       | ifthen ELSE EOL block {$$ = Me.AddElse($1, $4)}
       | elseif ELSE EOL block {$$ = Me.AddElse($1, $4)}
ifthen : IF expr EOL block                 {$$ = Me.CreateIfNode($2, $4)}
       | IF var ':' type EQ expr EOL block {$$ = Me.CreateIfCastNode($2, $4, $6, $8)}
elseif : ifthen ELSE ifthen    {$$ = Me.AddElse($1, Me.ToBlock($3))}
       | elseif ELSE ifthen    {$$ = Me.AddElse($1, Me.ToBlock($3))}


########## switch ##########
switch     : SWITCH expr EOL case_block {$$ = $4 : $4.Expression = $2 : $4.AppendLineNumber($1)}
case_block : BEGIN casen END           {$$ = $2}
casen      : case                      {$$ = Me.CreateSwitchNode($1)}
           | casen case                {$$ = $1 : $1.Case.Add($2)}
case       : case_expr ARROW EOL       {$$ = $1}
           | case_expr ARROW EOL block {$$ = $1 : $1.Then = $4}
           | case_expr ARROW expr EOL  {$$ = $1 : $1.Then = Me.ToLambdaExpression($3)}
case_expr  : var
           | num
           | str
           | var ':' type           {$$ = Me.CreateCaseCastNode($3, $1)}
           | '[' array_pattern ']'  {$$ = Me.CreateCaseArrayNode($2, $1)}
           | '(' tupple_pattern ')' {}

array_pattern  : patterns
tupple_pattern : patterns
patterns       : void                 {$$ = Me.CreateListNode(Of VariableNode)}
               | patternn extra
patternn       : pattern              {$$ = Me.CreateListNode($1)}
               | patternn ',' pattern {$1.List.Add($3) : $$ = $1}
pattern        : var

########## other ##########
var   : VAR     {$$ = Me.CreateVariableNode($1)}
varx  : var
      | SUB     {$$ = Me.CreateVariableNode($1)}
      | IF      {$$ = Me.CreateVariableNode($1)}
      | ELSE    {$$ = Me.CreateVariableNode($1)}
      | LET     {$$ = Me.CreateVariableNode($1)}
      | USE     {$$ = Me.CreateVariableNode($1)}
atvar : ATVAR   {$$ = New TypeNode(Me.CreateVariableNode($1)) With {.IsGeneric = True}}
num   : NUM     {$$ = $1}
str   : STR     {$$ = New StringNode($1)}
      | str STR {$1.String.Append($2.Name) : $$ = $1}
null  : NULL    {$$ = New NullNode($1)}
ope   : OPE     {$$ = New TokenNode($1)}
      | OR      {$$ = New TokenNode($1)}

extra : void
      | ','

void  : {$$ = Nothing}

