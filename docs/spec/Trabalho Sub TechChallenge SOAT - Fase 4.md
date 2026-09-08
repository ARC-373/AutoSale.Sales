# TRABALHO SUB TECH CHALLENGE CURSO SOAT – PÓSTECH FASE 4

Uma empresa de revenda de veículos automotores nos contratou pois quer implantar uma plataforma que funcione na internet, sendo assim, temos que criar a plataforma. O time de UX já está criando os designs, e ficou sob sua responsabilidade criar a API, para que posteriormente o time de frontend integre a solução. O desenho da solução envolve as seguintes necessidades do negócio:
- Cadastrar um veículo para venda (Marca, modelo, ano, cor, preço);
- Editar os dados do veículo;
- Efetuar a venda de um veículo (CPF da pessoa que comprou e data da venda);
- Disponibilizar um endpoint (webhook) para que a entidade que processa o pagamento consiga, a partir do código do pagamento, informar se o pagamento foi efetuado ou cancelado;
- Listagem de veículos à venda, ordenada por preço, do mais barato para o mais caro;
- Listagem de veículos vendidos, ordenada por preço, do mais barato para o mais caro.

Para suportar o aumento repentino de chamadas, os endpoints de listagem e compras de veículos devem estar isolados em um serviço único (serviço de venda de veículos), e rodando com um banco de dados isolado.

A comunicação entre o serviço de venda de veículos e o software principal deve ser feita via requisições http, respeitando os limites de atuação e responsabilidade de cada componente da solução.

Importante: nem todos os campos necessários estão descritos acima, por isso a modelagem é fundamental para entender como resolver o problema.
O time de qualidade de operação definiu que todas as mudanças da solução (implantação ou alteração) sejam feitas usando práticas de CI/CD, com Pull Requests e que as soluções que usam a arquitetura de microsserviços devem ter testes automatizados com todos os testes passando e com cobertura de testes de pelo menos 80%.

## Entregáveis
- PDF contendo os links de acesso aos itens abaixo:
    + 2 repositórios com o código funcionando (ver próximos itens).
        * 1 Repositório com o código-fonte do serviço de venda de veículos, com testes que tenham 80% de cobertura, o processo de CI/CD referente a ele, e o uso de um banco de dados segregado para este serviço;
        * 1 Repositório com o código-fonte da implementação das outras funcionalidades do software, com processo de CI/CD, com os deployments e services usados no processo de publicação do software e o código usando um banco de dados diferente do utilizado para o serviço de venda de veículos.
    + Vídeo demonstrando a solução funcionando, tanto na infraestrutura quanto no uso, fazendo um teste ponta-a-ponta, com cadastro de veículo para venda, compra e efetivação da compra, mostrando a implementação e o uso dos serviços funcionando de forma segregada, mas se comunicando, com os testes rodando e mostrando evidências da cobertura dos testes.
- Conteúdo e funcionalidades esperados em cada um dos repositórios:
    + Arquivo Readme.md que explique o que é o projeto, como foi implementado, como usar localmente e como testar;
    + Código-fonte de software que funcione corretamente e implemente todas as funcionalidades indicadas na descrição;
    + Deploy automatizado baseado em Pull Request, com o gatilho de execução vinculado a merge na Branch principal do projeto.