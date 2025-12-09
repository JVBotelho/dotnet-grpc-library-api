FROM caddy:builder AS builder

RUN xcaddy build \
    --with github.com/corazawaf/coraza-caddy/v2 \
    --with github.com/mholt/caddy-ratelimit \
    --output /usr/bin/caddy

FROM alpine:latest AS crs_downloader

RUN apk add --no-cache git
RUN git clone -b v4.0/dev https://github.com/coreruleset/coreruleset /owasp-crs
RUN cp /owasp-crs/crs-setup.conf.example /owasp-crs/crs-setup.conf

FROM caddy:latest

RUN apk add --no-cache dos2unix curl git

COPY --from=builder /usr/bin/caddy /usr/bin/caddy

RUN mkdir -p /etc/coraza/crs

COPY --from=crs_downloader /owasp-crs/crs-setup.conf /etc/coraza/crs/crs-setup.conf
COPY --from=crs_downloader /owasp-crs/rules /etc/coraza/crs/rules


RUN dos2unix /etc/coraza/crs/crs-setup.conf

EXPOSE 80 9090

CMD ["caddy", "run", "--config", "/etc/caddy/Caddyfile"]