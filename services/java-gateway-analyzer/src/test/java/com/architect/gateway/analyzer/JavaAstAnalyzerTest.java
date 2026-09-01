package com.architect.gateway.analyzer;

import com.architect.gateway.dto.AnalysisDtos.ResponseDto;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class JavaAstAnalyzerTest {

    private JavaAstAnalyzer analyzer;

    @BeforeEach
    void setUp() {
        analyzer = new JavaAstAnalyzer();
    }

    @Test
    void shouldDetectEmptyCatchBlockAndHardcodedSecret() {
        String testJavaCode = """
                package com.example;

                public class UserService {
                    private String dbPassword = "SecretAdminPassword123";

                    public void doWork() {
                        try {
                            int x = 10 / 0;
                        } catch (Exception e) {
                        }
                    }
                }
                """;

        ResponseDto result = analyzer.analyzeJavaSource("test-req-1", "UserService.java", testJavaCode);

        assertTrue(result.isSuccess());
        assertNotNull(result.getMetrics());
        assertEquals(1, result.getMetrics().getClassCount());
        assertEquals(1, result.getMetrics().getMethodCount());

        // Boş catch ve hardcoded secret tespit edilmeli
        boolean hasEmptyCatch = result.getViolations().stream()
                .anyMatch(v -> "ARCH-JAVA-001".equals(v.getRuleId()));
        boolean hasHardcodedSecret = result.getViolations().stream()
                .anyMatch(v -> "ARCH-SEC-001".equals(v.getRuleId()));

        assertTrue(hasEmptyCatch, "Empty catch block should be detected");
        assertTrue(hasHardcodedSecret, "Hardcoded secret should be detected");
    }

    @Test
    void shouldDetectTooManyParameters() {
        String testJavaCode = """
                package com.example;

                public class OrderService {
                    public void createOrder(String customerId, String productId, int quantity, double price, String address, String promoCode) {
                        System.out.println("Processing order");
                    }
                }
                """;

        ResponseDto result = analyzer.analyzeJavaSource("test-req-2", "OrderService.java", testJavaCode);

        assertTrue(result.isSuccess());
        boolean hasTooManyParams = result.getViolations().stream()
                .anyMatch(v -> "ARCH-JAVA-003".equals(v.getRuleId()));
        boolean hasSystemOut = result.getViolations().stream()
                .anyMatch(v -> "ARCH-JAVA-004".equals(v.getRuleId()));

        assertTrue(hasTooManyParams, "Too many parameters should be detected");
        assertTrue(hasSystemOut, "System.out.println usage should be detected");
    }
}
