package com.architect.gateway.controller;

import com.architect.gateway.analyzer.JavaAstAnalyzer;
import com.architect.gateway.config.RabbitMqConfig;
import com.architect.gateway.dto.AnalysisDtos.RequestDto;
import com.architect.gateway.dto.AnalysisDtos.ResponseDto;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;
import java.util.UUID;

@Slf4j
@RestController
@RequestMapping("/api/v1")
@RequiredArgsConstructor
public class WebhookController {

    private final JavaAstAnalyzer javaAstAnalyzer;
    private final RabbitTemplate rabbitTemplate;

    @GetMapping("/health")
    public ResponseEntity<Map<String, Object>> health() {
        return ResponseEntity.ok(Map.of(
                "service", "java-gateway-analyzer",
                "status", "UP",
                "timestamp", System.currentTimeMillis()
        ));
    }

    // Doğrudan Senkron Kod Analiz Endpoint'i
    @PostMapping("/analyze/sync")
    public ResponseEntity<ResponseDto> analyzeSync(@RequestBody RequestDto request) {
        if (request.getRequestId() == null) {
            request.setRequestId(UUID.randomUUID().toString());
        }

        log.info("Received sync analysis request for file: {}", request.getFilePath());

        if ("JAVA".equalsIgnoreCase(request.getLanguage()) || 
            (request.getFilePath() != null && request.getFilePath().endsWith(".java"))) {
            ResponseDto response = javaAstAnalyzer.analyzeJavaSource(
                    request.getRequestId(),
                    request.getFilePath(),
                    request.getSourceCode()
            );
            return ResponseEntity.ok(response);
        }

        // C# veya diğer diller için RabbitMQ üzerinden .NET servisine yönlendirilir
        return ResponseEntity.badRequest().body(ResponseDto.builder()
                .requestId(request.getRequestId())
                .success(false)
                .errorMessage("Language not handled synchronously by Java engine. Use async webhook queue.")
                .build());
    }

    // GitHub / GitLab Webhook Async Ingestion Endpoint'i
    @PostMapping("/webhooks/github")
    public ResponseEntity<Map<String, String>> handleGitHubWebhook(
            @RequestHeader(value = "X-GitHub-Event", defaultValue = "pull_request") String eventType,
            @RequestBody RequestDto request) {

        String requestId = UUID.randomUUID().toString();
        request.setRequestId(requestId);

        log.info("Ingested GitHub webhook event: {}, Request ID: {}", eventType, requestId);

        // RabbitMQ'ya kuyruğa at (Event-Driven Dağıtık İşlem)
        String routingKey = "JAVA".equalsIgnoreCase(request.getLanguage())
                ? RabbitMqConfig.ROUTING_KEY_JAVA
                : RabbitMqConfig.ROUTING_KEY_DOTNET;

        rabbitTemplate.convertAndSend(RabbitMqConfig.EXCHANGE_NAME, routingKey, request);

        return ResponseEntity.accepted().body(Map.of(
                "status", "QUEUED",
                "requestId", requestId,
                "routingKey", routingKey
        ));
    }
}
