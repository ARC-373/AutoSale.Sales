# TRABALHO SUB TECH CHALLENGE CURSO SOAT – PÓSTECH - FASE 3

Uma empresa de revenda de veículos automotores nos contratou pois quer implantar uma
plataforma que funcione na internet, sendo assim, temos que criar a plataforma. O time de UX já
está criando os designs, e ficou sob sua responsabilidade criar a API, para que posteriormente o
time de frontend integre a solução. O desenho da solução envolve as seguintes necessidades do
negócio:

- Cadastrar um veículo para venda (Marca, modelo, ano, cor, preço);
- Editar os dados do veículo;
- Permitir a compra do veículo via internet para pessoas cadastradas. O cadastro deve ser
feito anteriormente à compra do veículo;
- Listagem de veículos à venda, ordenada por preço, do mais barato para o mais caro;
- Listagem de veículos vendidos, ordenada por preço, do mais barato para o mais caro.

O processo de registro e autorização de compradores deve ser feito de forma separada, para
garantir que os dados de clientes estejam separados dos dados transacionais relacionados às
vendas dos veículos. Essa solução pode ser feita usando um serviço dedicado, como Auth0; pode
ser feita integrando serviços existentes como o Cognito; usando um serviço local, como o
Keycloak; ou implementar o processo de forma totalmente personalizada. Mas esse serviço deve
estar **totalmente apartado** do resto da solução.

*Importante*: nem todos os campos e funcionalidades necessárias para atender os requisitos
estão descritos acima, por isso a modelagem é fundamental para entender como resolver o
problema e entender o que precisa ser feito para que a solução funcione.

O time de qualidade de operação definiu que todas as mudanças da solução (implantação
ou alteração) sejam feitas usando práticas de CI/CD e com Pull Requests.

## Entregáveis

- PDF contendo os links de acesso aos itens abaixo:
    + Repositório com o código-fonte do software (ver próximo item);
    + Vídeo demonstrando a solução funcionando, tanto na infraestrutura quanto no uso, fazendo um teste início-a-fim, com cadastro de cliente, veículo, compra e efetivação da compra.
- Conteúdo e funcionalidades esperados em cada um dos repositórios:
    + Arquivo Readme.md que explique o que é o projeto, como foi implementado, como usar localmente e como testar;
    + Código-fonte de software que funcione corretamente e implemente todas as funcionalidades indicadas na descrição;
    + Deploy automatizado.