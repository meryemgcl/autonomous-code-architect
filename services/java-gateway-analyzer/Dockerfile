# Stage 1: Build stage
FROM maven:3.9.9-eclipse-temurin-21 AS build
WORKDIR /app

# Copy shared protos
COPY shared/protos ./shared/protos

# Copy Java service source
COPY services/java-gateway-analyzer ./services/java-gateway-analyzer
WORKDIR /app/services/java-gateway-analyzer

# Build and package the application
RUN mvn clean package -DskipTests

# Stage 2: Minimal Runtime stage
FROM eclipse-temurin:21-jre-alpine
WORKDIR /app

# Create non-root user for security best practices
RUN addgroup -S spring && adduser -S spring -G spring
USER spring:spring

COPY --from=build /app/services/java-gateway-analyzer/target/*.jar app.jar

EXPOSE 8080 9090
ENV JAVA_OPTS="-XX:+UseZGC -XX:MaxRAMPercentage=75"

ENTRYPOINT ["sh", "-c", "java $JAVA_OPTS -jar app.jar"]
