
Fire Up Rabbit

```ps
docker run `
    --name rabbitmq `
    -p 5672:5672 `
    -p 15672:15672 `
    -d `
    rabbitmq:4.0-management
```