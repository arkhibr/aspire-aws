#!/usr/bin/env bash
# Sobe LocalStack + Aspire Dashboard e fica no ar até Ctrl+C.
# Em outro terminal, rode os testes para ver atividade no dashboard.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo ""
echo "╔══════════════════════════════════════════════════════════╗"
echo "║           aspire-aws  —  modo demonstração               ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo ""
echo "  Subindo LocalStack + Aspire Dashboard..."
echo "  A URL do dashboard aparecerá abaixo em instantes."
echo ""
echo "  Em outro terminal, rode os testes:"
echo "    dotnet test scenarios/03-DynamoDB.Basic/"
echo "    dotnet test scenarios/16-Pipeline.Scheduler.Router/"
echo ""
echo "  Pressione Ctrl+C para parar tudo."
echo ""

# Garante que o container LocalStack seja removido ao sair
cleanup() {
    echo ""
    echo "  Encerrando Aspire e removendo container LocalStack..."
    docker rm -f aspire-aws-localstack 2>/dev/null || true
    echo "  Encerrado."
}
trap cleanup EXIT INT TERM

dotnet run --project "$ROOT/src/AppHost/" "$@"
