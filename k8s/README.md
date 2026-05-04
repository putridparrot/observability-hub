# Kubernetes Deployment Notes

## 0) Prerequisites

- Docker installed and running
- kubectl connected to your target AKS cluster
- Access to an Azure Container Registry (ACR)

## 1) Edit placeholders before build/deploy

Update these files first:

- `k8s/deployment.yaml`
	- Set `image` to your ACR image, for example:
	- `myregistry.azurecr.io/dashboard-web:latest`
- `k8s/ingress.yaml`
	- Set `host` to your real DNS entry
- `k8s/configmap.yaml`
	- Set `DashboardModules__AppInsightsKql__AppId`
- `k8s/secret.yaml`
	- Set `DashboardModules__AppInsightsKql__ApiKey`

## 2) Build and push image

Use your Azure Container Registry and tag the image used by `deployment.yaml`:

```bash
ACR_NAME=<your-acr-name>
ACR_LOGIN_SERVER=<your-acr-login-server>
IMAGE_TAG=latest

az acr login --name $ACR_NAME
docker build -t $ACR_LOGIN_SERVER/dashboard-web:$IMAGE_TAG .
docker push $ACR_LOGIN_SERVER/dashboard-web:$IMAGE_TAG
```

If you used a different tag, update `k8s/deployment.yaml` accordingly.

## 3) (Optional) Validate manifests before apply

```bash
kubectl kustomize k8s
```

## 4) Apply manifests

```bash
kubectl apply -k k8s
```

## 5) Verify rollout

```bash
kubectl -n dashboard get pods
kubectl -n dashboard rollout status deployment/dashboard-web
kubectl -n dashboard get svc,ingress
```

## 6) Optional ServiceMonitor

Only apply if Prometheus Operator CRDs are installed:

```bash
kubectl apply -f k8s/optional/servicemonitor.yaml
```
