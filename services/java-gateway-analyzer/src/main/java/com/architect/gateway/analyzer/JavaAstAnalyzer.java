package com.architect.gateway.analyzer;

import com.architect.gateway.dto.AnalysisDtos.*;
import com.github.javaparser.StaticJavaParser;
import com.github.javaparser.ast.CompilationUnit;
import com.github.javaparser.ast.body.ClassOrInterfaceDeclaration;
import com.github.javaparser.ast.body.MethodDeclaration;
import com.github.javaparser.ast.body.VariableDeclarator;
import com.github.javaparser.ast.expr.LiteralStringValueExpr;
import com.github.javaparser.ast.expr.MethodCallExpr;
import com.github.javaparser.ast.stmt.CatchClause;
import com.github.javaparser.ast.stmt.IfStmt;
import com.github.javaparser.ast.stmt.WhileStmt;
import com.github.javaparser.ast.stmt.ForStmt;
import com.github.javaparser.ast.stmt.ForEachStmt;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Pattern;

@Service
public class JavaAstAnalyzer {

    private static final Pattern SECRET_PATTERN = Pattern.compile("(?i)(password|secret|api_?key|bearer|token|private_?key)");

    public ResponseDto analyzeJavaSource(String requestId, String filePath, String sourceCode) {
        long startTime = System.currentTimeMillis();

        if (sourceCode == null || sourceCode.isBlank()) {
            return ResponseDto.builder()
                    .requestId(requestId)
                    .filePath(filePath)
                    .success(false)
                    .errorMessage("Java source code is empty")
                    .executionTimeMs(0)
                    .build();
        }

        try {
            CompilationUnit cu = StaticJavaParser.parse(sourceCode);
            List<ViolationDto> violations = new ArrayList<>();

            // 1. Sınıf & Metod Metrikleri
            List<ClassOrInterfaceDeclaration> classes = cu.findAll(ClassOrInterfaceDeclaration.class);
            List<MethodDeclaration> methods = cu.findAll(MethodDeclaration.class);

            int loc = sourceCode.split("\r\n|\r|\n").length;
            int complexity = 1;

            // Complexity hesaplama
            complexity += cu.findAll(IfStmt.class).size();
            complexity += cu.findAll(WhileStmt.class).size();
            complexity += cu.findAll(ForStmt.class).size();
            complexity += cu.findAll(ForEachStmt.class).size();
            complexity += cu.findAll(CatchClause.class).size();

            // Kural 1: Boş Catch Blokları
            for (CatchClause cc : cu.findAll(CatchClause.class)) {
                if (cc.getBody().getStatements().isEmpty()) {
                    int startLine = cc.getBegin().map(p -> p.line).orElse(1);
                    int endLine = cc.getEnd().map(p -> p.line).orElse(startLine);
                    violations.add(ViolationDto.builder()
                            .ruleId("ARCH-JAVA-001")
                            .ruleName("AvoidEmptyCatchBlocks")
                            .category("CODE_SMELL")
                            .severity("WARNING")
                            .startLine(startLine)
                            .endLine(endLine)
                            .description("Boş catch bloğu tespit edildi. İstisnaların yutulması sistemde sessiz hatalara neden olur.")
                            .suggestedFix("Hatayı SLF4J (log.error) ile loglayın veya uygun bir runtime exception fırlatın.")
                            .build());
                }
            }

            // Kural 2: Uzun Metodlar (> 30 satır) ve Fazla Parametre (> 4 parametre)
            for (MethodDeclaration md : methods) {
                int start = md.getBegin().map(p -> p.line).orElse(1);
                int end = md.getEnd().map(p -> p.line).orElse(start);
                int methodLength = end - start + 1;

                if (methodLength > 30) {
                    violations.add(ViolationDto.builder()
                            .ruleId("ARCH-JAVA-002")
                            .ruleName("AvoidLargeMethods")
                            .category("SOLID_PRINCIPLES")
                            .severity("WARNING")
                            .startLine(start)
                            .endLine(end)
                            .description("'" + md.getNameAsString() + "' metodu " + methodLength + " satır uzunluğunda. Single Responsibility ihlali olabilir.")
                            .suggestedFix("Metodu daha küçük ve odaklı private metodlara bölün.")
                            .build());
                }

                if (md.getParameters().size() > 4) {
                    violations.add(ViolationDto.builder()
                            .ruleId("ARCH-JAVA-003")
                            .ruleName("TooManyParameters")
                            .category("CODE_SMELL")
                            .severity("WARNING")
                            .startLine(start)
                            .endLine(end)
                            .description("'" + md.getNameAsString() + "' metodu " + md.getParameters().size() + " parametre alıyor. (Önerilen maksimum: 4)")
                            .suggestedFix("Parametreleri bir DTO/Command nesnesi içinde birleştirin.")
                            .build());
                }
            }

            // Kural 3: Sabit Kodlanmış Gizli Bilgiler (Hardcoded Secrets)
            for (VariableDeclarator vd : cu.findAll(VariableDeclarator.class)) {
                String varName = vd.getNameAsString();
                if (SECRET_PATTERN.matcher(varName).find()) {
                    vd.getInitializer().ifPresent(init -> {
                        if (init instanceof LiteralStringValueExpr strLiteral && strLiteral.getValue().length() > 4) {
                            int start = vd.getBegin().map(p -> p.line).orElse(1);
                            int end = vd.getEnd().map(p -> p.line).orElse(start);
                            violations.add(ViolationDto.builder()
                                    .ruleId("ARCH-SEC-001")
                                    .ruleName("HardcodedSecretDetected")
                                    .category("SECURITY")
                                    .severity("CRITICAL")
                                    .startLine(start)
                                    .endLine(end)
                                    .description("'" + varName + "' değişkeninde sabit kodlanmış gizli anahtar/parola tespit edildi.")
                                    .suggestedFix("Gizli anahtarları application.yml (@Value / @ConfigurationProperties) veya ortam değişkeninden okuyun.")
                                    .build());
                        }
                    });
                }
            }

            // Kural 4: System.out.println Kullanımı (Kurumsal kodda loglama standardı)
            for (MethodCallExpr mce : cu.findAll(MethodCallExpr.class)) {
                if (mce.getScope().isPresent() && mce.getScope().get().toString().equals("System.out")) {
                    int start = mce.getBegin().map(p -> p.line).orElse(1);
                    violations.add(ViolationDto.builder()
                            .ruleId("ARCH-JAVA-004")
                            .ruleName("AvoidSystemOutPrintln")
                            .category("CODE_SMELL")
                            .severity("INFO")
                            .startLine(start)
                            .endLine(start)
                            .description("Standart çıktı 'System.out' kullanılmış. Kurumsal projelerde log framework'ü kullanılmalıdır.")
                            .suggestedFix("SLF4J / Logback (örn. @Slf4j ve log.info()) kullanın.")
                            .build());
                }
            }

            int maintainability = Math.max(0, Math.min(100, 100 - (complexity * 3) - (loc / 10)));
            MetricsDto metricsDto = MetricsDto.builder()
                    .linesOfCode(loc)
                    .cyclomaticComplexity(complexity)
                    .maintainabilityIndex(maintainability)
                    .methodCount(methods.size())
                    .classCount(classes.size())
                    .build();

            long elapsed = System.currentTimeMillis() - startTime;
            return ResponseDto.builder()
                    .requestId(requestId)
                    .filePath(filePath)
                    .success(true)
                    .metrics(metricsDto)
                    .violations(violations)
                    .executionTimeMs(elapsed)
                    .build();

        } catch (Exception e) {
            long elapsed = System.currentTimeMillis() - startTime;
            return ResponseDto.builder()
                    .requestId(requestId)
                    .filePath(filePath)
                    .success(false)
                    .errorMessage("AST Parse Error: " + e.getMessage())
                    .executionTimeMs(elapsed)
                    .build();
        }
    }
}
