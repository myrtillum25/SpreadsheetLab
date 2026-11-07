// Grammar for variant 46
grammar LabCalculator;

/*
 * Parser Rules
 */

// Whole formula
compileUnit : EQUAL? expression EOF ;

// Higher in the list = higher priority
expression
    : LPAREN expression RPAREN                                         #ParenthesizedExpr
    | <assoc=right> expression EXPONENT expression                     #ExponentialExpr
    | SUBTRACT expression                                              #UnaryMinusExpr
    | expression operatorToken=(MULTIPLY | DIVIDE) expression          #MultiplicativeExpr
    | expression operatorToken=(ADD | SUBTRACT) expression             #AdditiveExpr
    | expression operatorToken=(EQUAL | NOT_EQUAL | LESS | GREATER | LESS_EQUAL | GREATER_EQUAL) expression   #ComparisonExpr
    | functionName=(MMAX | MMIN) LPAREN arguments+=expression (COMMA arguments+=expression)* RPAREN        #FunctionExpr
    | NUMBER                                                           #NumberExpr
    | IDENTIFIER                                                       #IdentifierExpr
    ;

/*
 * Lexer Rules
 */

MMAX : [mM][mM][aA][xX] ;
MMIN : [mM][mM][iI][nN] ;

NUMBER     : [0-9]+ ;                // any length
IDENTIFIER : [a-zA-Z] [1-9] [0-9]* ; // cell name: A1, b12
NAME       : [a-zA-Z]+ ;             // unknown word (for error message)

EXPONENT : '^' ;
MULTIPLY : '*' ;
DIVIDE   : '/' ;
ADD      : '+' ;
SUBTRACT : '-' ;

EQUAL         : '=' ;
NOT_EQUAL     : '<>' ;
LESS          : '<' ;
GREATER       : '>' ;
LESS_EQUAL    : '<=' ;
GREATER_EQUAL : '>=' ;

LPAREN : '(' ;
RPAREN : ')' ;
COMMA  : ',' ;

WS : [ \t\r\n]+ -> skip ;            // ignore spaces