package com.architect.gateway.grpc;

import com.architect.gateway.dto.AnalysisDtos.ResponseDto;
import com.architect.gateway.dto.AnalysisDtos.ViolationDto;
import com.architect.gateway.dto.AnalysisDtos.MetricsDto;
import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import jakarta.annotation.PostConstruct;
import jakarta.annotation.PreDestroy;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.TimeUnit;

@Slf4j
@Service
public class DotnetGrpcClient {

    @Value("${grpc.client.dotnet-engine.host:localhost}")
    private String dotnetHost;

    @Value("${grpc.client.dotnet-engine.port:5001}")
    private int dotnetPort;

    private ManagedChannel channel;
    private CodeAnalysisServiceGrpc.CodeAnalysisServiceBlockingStub blockingStub;

    @PostConstruct
    public void init() {
        log.info("Initializing gRPC Channel to .NET Engine at {}:{}", dotnetHost, dotnetPort);
        channel = ManagedChannelBuilder.forAddress(dotnetHost, dotnetPort)
                .usePlaintext()
                .build();
        blockingStub = CodeAnalysisServiceGrpc.newBlockingStub(channel);
    }

    @PreDestroy
    public void shutdown() {
        if (channel != null && !channel.isShutdown()) {
            try {
                channel.shutdown().awaitTermination(3, TimeUnit.SECONDS);
            } catch (InterruptedException e) {
                log.warn("Interrupted while shutting down gRPC channel", e);
                Thread.currentThread().interrupt();
            }
        }
    }

    public ResponseDto analyzeCSharpCode(String requestId, String filePath, String sourceCode, boolean enableAi) {
        log.info("Forwarding C# analysis request to .NET via gRPC: RequestId={}", requestId);

        CodeAnalysisRequest grpcRequest = CodeAnalysisRequest.newBuilder()
                .setRequestId(requestId)
                .setFilePath(filePath)
                .setSourceCode(sourceCode)
                .setLanguage(ProgrammingLanguage.LANGUAGE_CSHARP)
                .setEnableAiAgents(enableAi)
                .build();

        try {
            CodeAnalysisResponse grpcResponse = blockingStub.analyzeCodeSnippet(grpcRequest);

            List<ViolationDto> violations = new ArrayList<>();
            for (CodeViolation v : grpcResponse.getViolationsList()) {
                violations.add(ViolationDto.builder()
                        .ruleId(v.getRuleId())
                        .ruleName(v.getRuleName())
                        .category(v.getCategory())
                        .severity(v.getSeverity().name())
                        .startLine(v.getStartLine())
                        .endLine(v.getEndLine())
                        .description(v.getDescription())
                        .suggestedFix(v.getSuggestedFix())
                        .build());
            }

            MetricsDto metrics = MetricsDto.builder()
                    .linesOfCode(grpcResponse.getMetrics().getLinesOfCode())
                    .cyclomaticComplexity(grpcResponse.getMetrics().getCyclomaticComplexity())
                    .maintainabilityIndex(grpcResponse.getMetrics().getMaintainabilityIndex())
                    .methodCount(grpcResponse.getMetrics().getMethodCount())
                    .classCount(grpcResponse.getMetrics().getClassCount())
                    .build();

            return ResponseDto.builder()
                    .requestId(grpcResponse.getRequestId())
                    .filePath(filePath)
                    .success(grpcResponse.getSuccess())
                    .errorMessage(grpcResponse.getErrorMessage())
                    .metrics(metrics)
                    .violations(violations)
                    .executionTimeMs(grpcResponse.getExecutionTimeMs())
                    .build();

        } catch (Exception e) {
            log.error("Failed to execute gRPC call to .NET Engine: {}", e.getMessage());
            return ResponseDto.builder()
                    .requestId(requestId)
                    .filePath(filePath)
                    .success(false)
                    .errorMessage("gRPC connection error to .NET: " + e.getMessage())
                    .build();
        }
    }
}
