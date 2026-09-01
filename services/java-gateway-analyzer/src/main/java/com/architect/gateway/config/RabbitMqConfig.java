package com.architect.gateway.config;

import org.springframework.amqp.core.*;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.amqp.support.converter.MessageConverter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitMqConfig {

    public static final String EXCHANGE_NAME = "architect.code.exchange";
    public static final String JAVA_ANALYSIS_QUEUE = "architect.code.java.queue";
    public static final String DOTNET_ANALYSIS_QUEUE = "architect.code.dotnet.queue";
    public static final String ROUTING_KEY_JAVA = "code.analyze.java";
    public static final String ROUTING_KEY_DOTNET = "code.analyze.dotnet";

    @Bean
    public TopicExchange exchange() {
        return new TopicExchange(EXCHANGE_NAME);
    }

    @Bean
    public Queue javaQueue() {
        return QueueBuilder.durable(JAVA_ANALYSIS_QUEUE).build();
    }

    @Bean
    public Queue dotnetQueue() {
        return QueueBuilder.durable(DOTNET_ANALYSIS_QUEUE).build();
    }

    @Bean
    public Binding javaBinding(Queue javaQueue, TopicExchange exchange) {
        return BindingBuilder.bind(javaQueue).to(exchange).with(ROUTING_KEY_JAVA);
    }

    @Bean
    public Binding dotnetBinding(Queue dotnetQueue, TopicExchange exchange) {
        return BindingBuilder.bind(dotnetQueue).to(exchange).with(ROUTING_KEY_DOTNET);
    }

    @Bean
    public MessageConverter jsonMessageConverter() {
        return new Jackson2JsonMessageConverter();
    }

    @Bean
    public AmqpTemplate amqpTemplate(ConnectionFactory connectionFactory) {
        RabbitTemplate rabbitTemplate = new RabbitTemplate(connectionFactory);
        rabbitTemplate.setMessageConverter(jsonMessageConverter());
        return rabbitTemplate;
    }
}
